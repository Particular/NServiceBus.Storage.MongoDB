using System;
using MongoDB.Driver;
using NServiceBus.Configuration.AdvancedExtensibility;
using NServiceBus.Storage.MongoDB;

namespace NServiceBus
{
    public static class MongoSettingsExtensions
    {
        /// <summary>
        /// Override the default MongoClient creation by providing a pre-configured IMongoClient
        /// </summary>
        /// <param name="persistenceExtensions"></param>
        /// <param name="client"></param>
        /// <returns></returns>
        public static PersistenceExtensions<MongoPersistence> Client(this PersistenceExtensions<MongoPersistence> persistenceExtensions, IMongoClient client)
        {
            Guard.AgainstNull(nameof(persistenceExtensions), persistenceExtensions);
            Guard.AgainstNull(nameof(client), client);

            persistenceExtensions.GetSettings().Set(SettingsKeys.Client, (Func<IMongoClient>)(() => client));
            return persistenceExtensions;
        }

        /// <summary>
        /// Override the default database used by the persistence
        /// </summary>
        /// <param name="persistenceExtensions"></param>
        /// <param name="databaseName"></param>
        /// <returns></returns>
        public static PersistenceExtensions<MongoPersistence> DatabaseName(this PersistenceExtensions<MongoPersistence> persistenceExtensions, string databaseName)
        {
            Guard.AgainstNull(nameof(persistenceExtensions), persistenceExtensions);
            Guard.AgainstNullAndEmpty(nameof(databaseName), databaseName);

            persistenceExtensions.GetSettings().Set(SettingsKeys.DatabaseName, databaseName);
            return persistenceExtensions;
        }

        /// <summary>
        /// Configure whether the persistence should use MongoDB transactions
        /// </summary>
        /// <param name="persistenceExtensions"></param>
        /// <param name="useTransactions"></param>
        /// <returns></returns>
        public static PersistenceExtensions<MongoPersistence> UseTransactions(this PersistenceExtensions<MongoPersistence> persistenceExtensions, bool useTransactions)
        {
            Guard.AgainstNull(nameof(persistenceExtensions), persistenceExtensions);

            persistenceExtensions.GetSettings().Set(SettingsKeys.UseTransactions, useTransactions);
            return persistenceExtensions;
        }

        /// <summary>
        /// Community persistence compatibility settings
        /// </summary>
        /// <param name="persistenceExtensions"></param>
        /// <returns></returns>
        public static CompatibilitySettings CommunityPersistenceCompatibility(this PersistenceExtensions<MongoPersistence> persistenceExtensions) => new CompatibilitySettings(persistenceExtensions.GetSettings());
    }
}