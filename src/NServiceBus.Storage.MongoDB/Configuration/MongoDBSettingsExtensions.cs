using System;
using MongoDB.Driver;
using NServiceBus.Configuration.AdvancedExtensibility;
using NServiceBus.Storage.MongoDB;

namespace NServiceBus
{
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

        public static PersistenceExtensions<MongoDBPersistence> UseTransactions(this PersistenceExtensions<MongoDBPersistence> persistenceExtensions, bool useTransactions)
        {
            Guard.AgainstNull(nameof(persistenceExtensions), persistenceExtensions);

            persistenceExtensions.GetSettings().Set(SettingsKeys.UseTransactions, useTransactions);
            return persistenceExtensions;
        }

        public static CompatibilitySettings CommunityPersistenceCompatibility(this PersistenceExtensions<MongoDBPersistence> persistenceExtensions) => new CompatibilitySettings(persistenceExtensions.GetSettings());
    }
}