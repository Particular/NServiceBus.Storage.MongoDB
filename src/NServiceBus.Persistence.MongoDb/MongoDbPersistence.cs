namespace NServiceBus.Persistence.MongoDB
{
    using NServiceBus.Features;
    using NServiceBus.Persistence.MongoDB.Database;
    using NServiceBus.Persistence.MongoDB.Gateway;
    using NServiceBus.Persistence.MongoDB.Sagas;

    public class MongoDbPersistence : PersistenceDefinition
    {
        public MongoDbPersistence()
        {
            Defaults(s =>
            {
                s.EnableFeatureByDefault<MongoDbStorage>();
            });

            Supports<StorageType.GatewayDeduplication>(s => s.EnableFeatureByDefault<MongoDbGatewayDeduplication>());
            Supports<StorageType.Sagas>(s => s.EnableFeatureByDefault<MongoDbSagaStorage>());
        }
    }
}