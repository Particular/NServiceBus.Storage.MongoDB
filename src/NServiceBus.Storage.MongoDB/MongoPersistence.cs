namespace NServiceBus
{
    using System;
    using Features;
    using MongoDB.Bson;
    using MongoDB.Bson.Serialization;
    using MongoDB.Bson.Serialization.Serializers;
    using MongoDB.Driver;
    using Persistence;
    using Storage.MongoDB;

    /// <summary>
    /// Used to configure NServiceBus to use MongoDB persistence.
    /// </summary>
    public class MongoPersistence : PersistenceDefinition
    {
        /// <summary>
        /// Creates a new instance of the persistence definition.
        /// </summary>
        public MongoPersistence()
        {
            BsonSerializer.TryRegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));

            Defaults(s =>
            {
                s.SetDefault(SettingsKeys.MongoClient, () =>
                {
                    defaultClient ??= new MongoClient();

                    return defaultClient;
                });

                s.SetDefault(SettingsKeys.DatabaseName, s.EndpointName());

                s.SetDefault(SettingsKeys.CollectionNamingConvention, DefaultCollectionNamingConvention);

                s.SetDefault(DefaultDatabaseSettings);
            });

            Supports<StorageType.Sagas>(s => s.EnableFeatureByDefault<SagaStorage>());
            Supports<StorageType.Outbox>(s => s.EnableFeatureByDefault<OutboxStorage>());
            Supports<StorageType.Subscriptions>(s => s.EnableFeatureByDefault<SubscriptionStorage>());
        }

        internal static MongoDatabaseSettings DefaultDatabaseSettings { get; } = new MongoDatabaseSettings
        {
            ReadConcern = ReadConcern.Majority,
            WriteConcern = WriteConcern.WMajority,
            ReadPreference = ReadPreference.Primary
        };

        internal static readonly TimeSpan DefaultTransactionTimeout = TimeSpan.FromSeconds(60);
        internal static readonly Func<Type, string> DefaultCollectionNamingConvention = type => type.Name.ToLower();

        static IMongoClient defaultClient;
    }
}