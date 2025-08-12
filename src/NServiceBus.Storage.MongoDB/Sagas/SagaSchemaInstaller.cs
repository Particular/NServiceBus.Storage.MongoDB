namespace NServiceBus.Storage.MongoDB;

using System;
using System.Threading;
using System.Threading.Tasks;
using global::MongoDB.Bson;
using global::MongoDB.Bson.Serialization;
using global::MongoDB.Driver;
using Installation;
using Sagas;
using Settings;

sealed class SagaSchemaInstaller(IReadOnlySettings settings, InstallerSettings installerSettings) : INeedToInstallSomething
{
    public Task Install(string identity, CancellationToken cancellationToken = default)
    {
        if (installerSettings.Disabled || !settings.TryGet<Func<IMongoClient>>(SettingsKeys.MongoClient, out Func<IMongoClient>? client))
        {
            return Task.CompletedTask;
        }

        var databaseName = settings.Get<string>(SettingsKeys.DatabaseName);
        var collectionNamingConvention = settings.Get<Func<Type, string>>(SettingsKeys.CollectionNamingConvention);
        var sagaMetadataCollection = settings.Get<SagaMetadataCollection>();
        var databaseSettings = settings.Get<MongoDatabaseSettings>();

        var memberMapCache = new MemberMapCache();
        InitializeSagaDataTypes(client(), databaseSettings, memberMapCache, databaseName, collectionNamingConvention, sagaMetadataCollection);

        return Task.CompletedTask;
    }

    internal static void InitializeSagaDataTypes(IMongoClient client, MongoDatabaseSettings databaseSettings, MemberMapCache memberMapCache,
        string databaseName, Func<Type, string> collectionNamingConvention,
        SagaMetadataCollection sagaMetadataCollection)
    {
        IMongoDatabase? database = client.GetDatabase(databaseName, databaseSettings);

        foreach (var sagaMetadata in sagaMetadataCollection)
        {
            if (!BsonClassMap.IsClassMapRegistered(sagaMetadata.SagaEntityType))
            {
                var classMap = new BsonClassMap(sagaMetadata.SagaEntityType);
                classMap.AutoMap();
                classMap.SetIgnoreExtraElements(true);

                BsonClassMap.RegisterClassMap(classMap);
            }

            string collectionName = collectionNamingConvention(sagaMetadata.SagaEntityType);

            if (sagaMetadata.TryGetCorrelationProperty(out SagaMetadata.CorrelationPropertyMetadata? property) &&
                property.Name != "Id")
            {
                var memberMap = memberMapCache.GetOrAdd(sagaMetadata.SagaEntityType, property);
                var propertyElementName = memberMap.ElementName;

                var indexModel = new CreateIndexModel<BsonDocument>(
                    new BsonDocumentIndexKeysDefinition<BsonDocument>(new BsonDocument(propertyElementName, 1)),
                    new CreateIndexOptions { Unique = true });
                // TODO Should we use the collection settings from the saga metadata?
                database.GetCollection<BsonDocument>(collectionName).Indexes.CreateOne(indexModel);
            }
            else
            {
                try
                {
                    database.CreateCollection(collectionName);
                }
                catch (MongoCommandException ex) when (ex.Code == 48 && ex.CodeName == "NamespaceExists")
                {
                    //Collection already exists, so swallow the exception
                }
            }
        }
    }
}