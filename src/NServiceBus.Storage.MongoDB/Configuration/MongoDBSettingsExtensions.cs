namespace NServiceBus
{
    using System;
    using Configuration.AdvancedExtensibility;
    using MongoDB.Driver;
    using Storage.MongoDB;

    public static class MongoDBSettingsExtensions
    {
        public static PersistenceExtensions<MongoDBPersistence> Client(this PersistenceExtensions<MongoDBPersistence> persistenceExtensions, IMongoClient client)
        {
            Guard.AgainstNull(nameof(persistenceExtensions), persistenceExtensions);
            Guard.AgainstNull(nameof(client), client);

            persistenceExtensions.GetSettings().Set(SettingsKeys.Client, (Func<IMongoClient>)(() => client));
            return persistenceExtensions;
        }

        public static PersistenceExtensions<MongoDBPersistence> DatabaseName(this PersistenceExtensions<MongoDBPersistence> persistenceExtensions, string databaseName)
        {
            Guard.AgainstNull(nameof(persistenceExtensions), persistenceExtensions);
            Guard.AgainstNullAndEmpty(nameof(databaseName), databaseName);

            persistenceExtensions.GetSettings().Set(SettingsKeys.DatabaseName, databaseName);
            return persistenceExtensions;
        }

        public static PersistenceExtensions<MongoDBPersistence> VersionFieldName(this PersistenceExtensions<MongoDBPersistence> persistenceExtensions, string versionFieldnName)
        {
            persistenceExtensions.GetSettings().Set(SettingsKeys.VersionFieldName, versionFieldnName);
            return persistenceExtensions;
        }

        public static PersistenceExtensions<MongoDBPersistence> CollectionNamingScheme(this PersistenceExtensions<MongoDBPersistence> persistenceExtensions, Func<Type, string> collectionNamingScheme)
        {
            Guard.AgainstNull(nameof(persistenceExtensions), persistenceExtensions);
            Guard.AgainstNull(nameof(collectionNamingScheme), collectionNamingScheme);
            
            //TODO: make sure null isn't returned or throw with collectionNamingScheme

            persistenceExtensions.GetSettings().Set(SettingsKeys.CollectionNamingScheme, collectionNamingScheme);
            return persistenceExtensions;
        }
    }
}