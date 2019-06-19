using System;
using MongoDB.Driver;
using NServiceBus.Configuration.AdvancedExtensibility;
using NServiceBus.Storage.MongoDB;

namespace NServiceBus
{
    /// <summary>
    ///
    /// </summary>
    public static class MongoSettingsExtensions
    {
        /// <summary>
        /// Override the default MongoClient creation by providing a pre-configured IMongoClient
        /// </summary>
        /// <param name="persistenceExtensions"></param>
        /// <param name="mongoClient"></param>
        /// <returns></returns>
        public static PersistenceExtensions<MongoPersistence> MongoClient(this PersistenceExtensions<MongoPersistence> persistenceExtensions, IMongoClient mongoClient)
        {
            Guard.AgainstNull(nameof(persistenceExtensions), persistenceExtensions);
            Guard.AgainstNull(nameof(mongoClient), mongoClient);

            persistenceExtensions.GetSettings().Set(SettingsKeys.MongoClient, (Func<IMongoClient>)(() => mongoClient));
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
        /// Configure the time to live for completed outbox messages
        /// </summary>
        /// <param name="persistenceExtensions"></param>
        /// <param name="timeToLive">A non-negative TimeSpan</param>
        /// <returns></returns>
        public static PersistenceExtensions<MongoPersistence> OutboxTimeToLive(this PersistenceExtensions<MongoPersistence> persistenceExtensions, TimeSpan timeToLive)
        {
            Guard.AgainstNull(nameof(persistenceExtensions), persistenceExtensions);

            persistenceExtensions.GetSettings().Set(SettingsKeys.OutboxTimeSpan, timeToLive);
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