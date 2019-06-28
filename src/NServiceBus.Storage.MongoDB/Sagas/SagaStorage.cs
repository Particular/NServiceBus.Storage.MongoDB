using System;
using MongoDB.Bson;
using MongoDB.Driver;
using NServiceBus.Features;
using NServiceBus.Sagas;

namespace NServiceBus.Storage.MongoDB
{
    class SagaStorage : Feature
    {
        SagaStorage()
        {
            DependsOn<Features.Sagas>();
            DependsOn<SynchronizedStorage>();
        }

        protected override void Setup(FeatureConfigurationContext context)
        {
            if (!context.Settings.TryGet(SettingsKeys.VersionElementName, out string versionElementName))
            {
                versionElementName = "_version";
            }

            var client = context.Settings.Get<Func<IMongoClient>>(SettingsKeys.MongoClient)();
            var databaseName = context.Settings.Get<string>(SettingsKeys.DatabaseName);
            var database = client.GetDatabase(databaseName);
            var collectionNamingConvention = context.Settings.Get<Func<Type, string>>(SettingsKeys.CollectionNamingConvention);
            var sagaMetadataCollection = context.Settings.Get<SagaMetadataCollection>();

            CreateIndexes(database, collectionNamingConvention, sagaMetadataCollection);

            context.Container.ConfigureComponent(() => new SagaPersister(versionElementName), DependencyLifecycle.SingleInstance);
        }

        internal static void CreateIndexes(IMongoDatabase database, Func<Type, string> collectionNamingConvention, SagaMetadataCollection sagaMetadataCollection)
        {
            foreach (var sagaMetadata in sagaMetadataCollection)
            {
                var collectionName = collectionNamingConvention(sagaMetadata.SagaEntityType);

                if (sagaMetadata.TryGetCorrelationProperty(out var property))
                {
                    var propertyElementName = sagaMetadata.SagaEntityType.GetElementName(property.Name);

                    var indexModel = new CreateIndexModel<BsonDocument>(new BsonDocumentIndexKeysDefinition<BsonDocument>(new BsonDocument(propertyElementName, 1)), new CreateIndexOptions() { Unique = true });
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
}