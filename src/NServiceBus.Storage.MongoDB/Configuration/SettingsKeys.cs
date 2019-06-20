namespace NServiceBus.Storage.MongoDB
{
    static class SettingsKeys
    {
        const string baseName = "MongoDB.";
        public const string VersionElementName = baseName + nameof(VersionElementName);
        public const string CollectionNamingConvention = baseName + nameof(CollectionNamingConvention);
        public const string DatabaseName = baseName + nameof(DatabaseName);
        public const string MongoClient = baseName + nameof(MongoClient);
        public const string UseTransactions = baseName + nameof(UseTransactions);
        public const string TimeToKeepOutboxDeduplicationData = baseName + nameof(TimeToKeepOutboxDeduplicationData);
    }
}