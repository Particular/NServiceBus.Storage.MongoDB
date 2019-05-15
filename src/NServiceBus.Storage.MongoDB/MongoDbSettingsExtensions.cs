namespace NServiceBus
{
    using NServiceBus.Configuration.AdvanceExtensibility;
    using NServiceBus.Storage.MongoDB;

    public static class MongoDbSettingsExtensions
    {
        public static PersistenceExtensions<MongoDbPersistence> SetConnectionStringName(
            this PersistenceExtensions<MongoDbPersistence> cfg, string connectionStringName)
        {
            cfg.GetSettings().Set(MongoPersistenceSettings.ConnectionStringName, connectionStringName);
            return cfg;
        }

        public static PersistenceExtensions<MongoDbPersistence> SetConnectionString(
            this PersistenceExtensions<MongoDbPersistence> cfg, string connectionString)
        {
            cfg.GetSettings().Set(MongoPersistenceSettings.ConnectionString, connectionString);
            return cfg;
        }

    }
}