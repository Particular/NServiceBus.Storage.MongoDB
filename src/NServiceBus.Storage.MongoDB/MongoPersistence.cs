namespace NServiceBus
{
    using System;
    using Features;
    using MongoDB.Driver;
    using Persistence;
    using Storage.MongoDB;

    /// <summary>
    /// </summary>
    public class MongoPersistence : PersistenceDefinition
    {
        /// <summary>
        /// </summary>
        public MongoPersistence()
        {
            Defaults(s =>
            {
                s.EnableFeatureByDefault<SynchronizedStorage>();

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

        static IMongoClient defaultClient;
    }
}