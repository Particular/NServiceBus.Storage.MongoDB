namespace NServiceBus
{
    using NServiceBus.Configuration.AdvancedExtensibility;
    using NServiceBus.Storage.MongoDB;

    public static class MongoDBSettingsExtensions
    {
        public static PersistenceExtensions<MongoDBPersistence> SetConnectionStringName(this PersistenceExtensions<MongoDBPersistence> cfg, string connectionStringName)
        {
            cfg.GetSettings().Set(SettingsKeys.ConnectionStringName, connectionStringName);
            return cfg;
        }

        public static PersistenceExtensions<MongoDBPersistence> SetConnectionString(this PersistenceExtensions<MongoDBPersistence> cfg, string connectionString)
        {
            cfg.GetSettings().Set(SettingsKeys.ConnectionString, connectionString);
            return cfg;
        }
    }
}