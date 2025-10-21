namespace NServiceBus.Storage.MongoDB;

using System;
using System.Threading;
using System.Threading.Tasks;
using global::MongoDB.Bson;
using global::MongoDB.Driver;
using Installation;
using Sagas;
using Settings;

sealed class SagaInstaller(IReadOnlySettings settings, IMongoClientProvider clientProvider) : INeedToInstallSomething
{
    public async Task Install(string identity, CancellationToken cancellationToken = default)
    {
        var databaseName = settings.Get<string>(SettingsKeys.DatabaseName);
        var collectionNamingConvention = settings.Get<Func<Type, string>>(SettingsKeys.CollectionNamingConvention);
        var sagaMetadataCollection = settings.Get<SagaMetadataCollection>();
        var databaseSettings = settings.Get<MongoDatabaseSettings>();
        var collectionSettings = settings.Get<MongoCollectionSettings>();
        var memberMapCache = settings.Get<MemberMapCache>();

        await CreateInfrastructureForSagaDataTypes(clientProvider.Client, databaseSettings, memberMapCache, databaseName, collectionNamingConvention, collectionSettings, sagaMetadataCollection, cancellationToken)
            .ConfigureAwait(false);
    }

    internal static async Task CreateInfrastructureForSagaDataTypes(IMongoClient client, MongoDatabaseSettings databaseSettings,
        MemberMapCache memberMapCache,
        string databaseName, Func<Type, string> collectionNamingConvention,
        MongoCollectionSettings collectionSettings,
        SagaMetadataCollection sagaMetadataCollection,
        CancellationToken cancellationToken = default)
    {
        var database = client.GetDatabase(databaseName, databaseSettings);

        foreach (var sagaMetadata in sagaMetadataCollection)
        {
            string collectionName = collectionNamingConvention(sagaMetadata.SagaEntityType);

            if (sagaMetadata.TryGetCorrelationProperty(out SagaMetadata.CorrelationPropertyMetadata? property) &&
                property.Name != "Id")
            {
                var memberMap = memberMapCache.GetOrAdd(sagaMetadata.SagaEntityType, property);
                var propertyElementName = memberMap.ElementName;

                var indexModel = new CreateIndexModel<BsonDocument>(
                    new BsonDocumentIndexKeysDefinition<BsonDocument>(new BsonDocument(propertyElementName, 1)),
                    new CreateIndexOptions { Unique = true });
                await database.GetCollection<BsonDocument>(collectionName, collectionSettings).Indexes.CreateOneAsync(indexModel, cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
            }
            else
            {
                await database.SafeCreateCollection(collectionName, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}