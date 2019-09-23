namespace NServiceBus
{
    using System;
    using Configuration.AdvancedExtensibility;
    using MongoDB.Driver;
    using Storage.MongoDB;

    /// <summary>
    /// Extension methods to configure the MongoDB persistence.
    /// </summary>
    public static class MongoSettingsExtensions
    {
        /// <summary>
        /// Override the default MongoClient creation by providing a pre-configured IMongoClient
        /// </summary>
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
        public static PersistenceExtensions<MongoPersistence> UseTransactions(this PersistenceExtensions<MongoPersistence> persistenceExtensions, bool useTransactions)
        {
            Guard.AgainstNull(nameof(persistenceExtensions), persistenceExtensions);

            persistenceExtensions.GetSettings().Set(SettingsKeys.UseTransactions, useTransactions);
            return persistenceExtensions;
        }

        /// <summary>
        /// Configures the amount of time to keep outbox deduplication data.
        /// </summary>
        public static PersistenceExtensions<MongoPersistence> TimeToKeepOutboxDeduplicationData(this PersistenceExtensions<MongoPersistence> persistenceExtensions, TimeSpan timeToKeepOutboxDeduplicationData)
        {
            Guard.AgainstNull(nameof(persistenceExtensions), persistenceExtensions);
            Guard.AgainstNegativeAndZero(nameof(timeToKeepOutboxDeduplicationData), timeToKeepOutboxDeduplicationData);

            var seconds = Math.Ceiling(timeToKeepOutboxDeduplicationData.TotalSeconds);

            persistenceExtensions.GetSettings().Set(SettingsKeys.TimeToKeepOutboxDeduplicationData, TimeSpan.FromSeconds(seconds));
            return persistenceExtensions;
        }

        /// <summary>
        /// Community persistence compatibility settings
        /// </summary>
        public static CompatibilitySettings CommunityPersistenceCompatibility(this PersistenceExtensions<MongoPersistence> persistenceExtensions) => new CompatibilitySettings(persistenceExtensions.GetSettings());
    }
}