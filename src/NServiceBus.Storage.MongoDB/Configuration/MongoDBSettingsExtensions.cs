namespace NServiceBus
{
    using System;
    using Configuration.AdvancedExtensibility;
    using Storage.MongoDB;

    public static class MongoDBSettingsExtensions
    {
        public static PersistenceExtensions<MongoDBPersistence> ConnectionString(this PersistenceExtensions<MongoDBPersistence> persistenceExtensions, string connectionString)
        {
            persistenceExtensions.GetSettings().Set(SettingsKeys.ConnectionString, connectionString);
            return persistenceExtensions;
        }

        public static PersistenceExtensions<MongoDBPersistence> VersionFieldName(this PersistenceExtensions<MongoDBPersistence> persistenceExtensions, string versionFieldnName)
        {
            persistenceExtensions.GetSettings().Set(SettingsKeys.VersionFieldName, versionFieldnName);
            return persistenceExtensions;
        }

        public static PersistenceExtensions<MongoDBPersistence> CollectionNamingScheme(this PersistenceExtensions<MongoDBPersistence> persistenceExtensions, Func<Type, string> collectionNamingScheme)
        {
            persistenceExtensions.GetSettings().Set(SettingsKeys.CollectionNamingScheme, collectionNamingScheme);
            return persistenceExtensions;
        }
    }
}