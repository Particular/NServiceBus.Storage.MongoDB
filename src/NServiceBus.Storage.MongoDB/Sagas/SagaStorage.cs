using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
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

            var collectionIndexKeys = new Dictionary<string, List<string>>();

            var client = context.Settings.Get<Func<IMongoClient>>(SettingsKeys.MongoClient)();
            var databaseName = context.Settings.Get<string>(SettingsKeys.DatabaseName);

            var database = client.GetDatabase(databaseName);
            var collectionNames = database.ListCollectionNames().ToList();

            foreach (var name in collectionNames)
            {
                collectionIndexKeys.Add(name, new List<string>());

                var indexes = database.GetCollection<BsonDocument>(name).Indexes.List().ToList();

                foreach (var index in indexes)
                {
                    collectionIndexKeys[name].AddRange(index["key"].AsBsonDocument.Names);
                }
            }

            var collectionNamingConvention = context.Settings.Get<Func<Type, string>>(SettingsKeys.CollectionNamingConvention);
            var sagaMetadataCollection = context.Settings.Get<SagaMetadataCollection>();

            foreach (var sagaMetadata in sagaMetadataCollection)
            {
                var expectedCollectionName = collectionNamingConvention(sagaMetadata.SagaEntityType);
                var collectionExists = collectionIndexKeys.ContainsKey(expectedCollectionName);

                if (!collectionExists)
                {
                    database.CreateCollection(expectedCollectionName);
                }

                if (sagaMetadata.TryGetCorrelationProperty(out var property))
                {
                    var propertyElementName = sagaMetadata.SagaEntityType.GetElementName(property.Name);

                    if (!collectionExists || !collectionIndexKeys[expectedCollectionName].Contains(propertyElementName))
                    {
                        var indexModel = new CreateIndexModel<BsonDocument>(new BsonDocumentIndexKeysDefinition<BsonDocument>(new BsonDocument(propertyElementName, 1)), new CreateIndexOptions() { Unique = true });
                        database.GetCollection<BsonDocument>(expectedCollectionName).Indexes.CreateOne(indexModel);
                    }
                }
            }

            context.Container.ConfigureComponent(() => new SagaPersister(versionElementName), DependencyLifecycle.SingleInstance);
        }
    }
}