namespace NServiceBus.Persistence.MongoDB
{
    using NServiceBus.Features;
    using NServiceBus.Persistence.MongoDB.Database;
    using NServiceBus.Persistence.MongoDB.Gateway;
    using NServiceBus.Persistence.MongoDB.Sagas;
    using NServiceBus.Persistence.MongoDB.Subscriptions;
    using NServiceBus.Persistence.MongoDB.Timeout;

    public class MongoDbPersistence : PersistenceDefinition
    {
        public MongoDbPersistence()
        {
            Defaults(s =>
            {
                s.EnableFeatureByDefault<MongoDbStorage>();
            });

            Supports<StorageType.GatewayDeduplication>(s => s.EnableFeatureByDefault<MongoDbGatewayDeduplication>());
            Supports<StorageType.Timeouts>(s => s.EnableFeatureByDefault<MongoDbTimeoutStorage>());
            Supports<StorageType.Sagas>(s => s.EnableFeatureByDefault<MongoDbSagaStorage>());
            Supports<StorageType.Subscriptions>(s => s.EnableFeatureByDefault<MongoDbSubscriptionStorage>());
        }
    }
}