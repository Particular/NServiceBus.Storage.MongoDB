namespace NServiceBus
{
    using System;
    using Features;
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
            Defaults(s =>
            {
                s.SetDefault(SettingsKeys.MongoClient, (Func<IMongoClient>)(() =>
                {
                    if (defaultClient == null)
                    {
                        defaultClient = new MongoClient();
                    }

                    return defaultClient;
                }));

                s.SetDefault(SettingsKeys.DatabaseName, s.EndpointName());

                s.SetDefault(SettingsKeys.CollectionNamingConvention, (Func<Type, string>)(type => type.Name.ToLower()));

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

        static IMongoClient defaultClient;
    }
}