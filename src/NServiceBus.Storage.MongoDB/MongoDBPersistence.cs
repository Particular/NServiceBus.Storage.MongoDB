﻿namespace NServiceBus
{
    using NServiceBus.Features;
    using NServiceBus.Persistence;
    using NServiceBus.Storage.MongoDB;

    public class MongoDBPersistence : PersistenceDefinition
    {
        public MongoDBPersistence()
        {
            Defaults(s =>
            {
                s.EnableFeatureByDefault<MongoDBStorage>();
            });

            Supports<StorageType.Sagas>(s => s.EnableFeatureByDefault<SagaStorage>());
        }
    }
}