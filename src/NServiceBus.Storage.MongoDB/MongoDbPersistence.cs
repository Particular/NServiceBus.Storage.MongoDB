namespace NServiceBus
{
    using NServiceBus.Features;
    using NServiceBus.Persistence;
    using NServiceBus.Storage.MongoDB;

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