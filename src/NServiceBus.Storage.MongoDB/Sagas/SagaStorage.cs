namespace NServiceBus.Storage.MongoDB
{
    using System;
    using Features;
    using global::MongoDB.Bson;
    using global::MongoDB.Bson.Serialization;
    using global::MongoDB.Driver;
    using Sagas;

    class SagaStorage : Feature
    {
        SagaStorage()
        {
            DependsOn<Sagas>();
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

            var databaseSettings = new MongoDatabaseSettings
            {
                ReadConcern = ReadConcern.Majority,
                ReadPreference = ReadPreference.Primary,
                WriteConcern = WriteConcern.WMajority
            };

            var database = client.GetDatabase(databaseName, databaseSettings);
            var collectionNamingConvention = context.Settings.Get<Func<Type, string>>(SettingsKeys.CollectionNamingConvention);
            var sagaMetadataCollection = context.Settings.Get<SagaMetadataCollection>();

            InitializeSagaDataTypes(database, collectionNamingConvention, sagaMetadataCollection);

            context.Container.ConfigureComponent(() => new SagaPersister(versionElementName), DependencyLifecycle.SingleInstance);
        }

        internal static void InitializeSagaDataTypes(IMongoDatabase database, Func<Type, string> collectionNamingConvention, SagaMetadataCollection sagaMetadataCollection)
        {
            foreach (var sagaMetadata in sagaMetadataCollection)
            {
                if (!BsonClassMap.IsClassMapRegistered(sagaMetadata.SagaEntityType))
                {
                    var classMap = new BsonClassMap(sagaMetadata.SagaEntityType);
                    classMap.AutoMap();
                    classMap.SetIgnoreExtraElements(true);

                    BsonClassMap.RegisterClassMap(classMap);
                }

                var collectionName = collectionNamingConvention(sagaMetadata.SagaEntityType);

                if (sagaMetadata.TryGetCorrelationProperty(out var property) && property.Name != "Id")
                {
                    var propertyElementName = sagaMetadata.SagaEntityType.GetElementName(property.Name);

                    var indexModel = new CreateIndexModel<BsonDocument>(new BsonDocumentIndexKeysDefinition<BsonDocument>(new BsonDocument(propertyElementName, 1)), new CreateIndexOptions
                        {Unique = true});
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