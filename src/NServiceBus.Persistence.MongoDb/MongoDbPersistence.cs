namespace NServiceBus.Persistence.MongoDB
{
    using NServiceBus.Features;
    using NServiceBus.Persistence.MongoDB.Database;
    using NServiceBus.Persistence.MongoDB.Sagas;

    public class MongoDbPersistence : PersistenceDefinition
    {
        public MongoDbPersistence()
        {
            Defaults(s =>
            {
                s.EnableFeatureByDefault<MongoDbStorage>();
            });

            Supports<StorageType.Sagas>(s => s.EnableFeatureByDefault<MongoDbSagaStorage>());
        }
    }
}