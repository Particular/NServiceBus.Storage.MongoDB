namespace NServiceBus.Storage.MongoDB
{
    using System;
    using Features;
    using global::MongoDB.Bson;
    using global::MongoDB.Bson.Serialization;
    using global::MongoDB.Bson.Serialization.Serializers;
    using global::MongoDB.Driver;
    using Microsoft.Extensions.DependencyInjection;
    using Sagas;

    class SagaStorage : Feature
    {
        SagaStorage()
        {
            Defaults(s => s.EnableFeatureByDefault<SynchronizedStorage>());

            DependsOn<Sagas>();
            DependsOn<SynchronizedStorage>();
        }

        protected override void Setup(FeatureConfigurationContext context)
        {
            if (!context.Settings.TryGet(SettingsKeys.VersionElementName, out string versionElementName))
            {
                versionElementName = SagaPersister.DefaultVersionElementName;
            }

            var client = context.Settings.Get<Func<IMongoClient>>(SettingsKeys.MongoClient)();
            var databaseName = context.Settings.Get<string>(SettingsKeys.DatabaseName);
            var collectionNamingConvention = context.Settings.Get<Func<Type, string>>(SettingsKeys.CollectionNamingConvention);
            var sagaMetadataCollection = context.Settings.Get<SagaMetadataCollection>();

            InitializeSagaDataTypes(client, databaseName, collectionNamingConvention, sagaMetadataCollection);

            context.Services.AddSingleton<ISagaPersister>(new SagaPersister(versionElementName));
        }

        internal static void InitializeSagaDataTypes(IMongoClient client, string databaseName, Func<Type, string> collectionNamingConvention, SagaMetadataCollection sagaMetadataCollection)
        {
            BsonSerializer.TryRegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));
            BsonSerializer.TryRegisterSerializer(new ObjectSerializer(BsonSerializer.LookupDiscriminatorConvention(typeof(object)), GuidRepresentation.Standard));

            var databaseSettings = new MongoDatabaseSettings
            {
                ReadConcern = ReadConcern.Majority,
                ReadPreference = ReadPreference.Primary,
                WriteConcern = WriteConcern.WMajority
            };
            var database = client.GetDatabase(databaseName, databaseSettings);

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
                    { Unique = true });
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