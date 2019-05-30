namespace NServiceBus.Storage.MongoDB
{
    static class SettingsKeys
    {
        const string baseName = "MongoDB.";
        public const string VersionElementName = baseName + nameof(VersionElementName);
        public const string CollectionNamingConvention = baseName + nameof(CollectionNamingConvention);
        public const string DatabaseName = baseName + nameof(DatabaseName);
        public const string Client = baseName + nameof(Client);
        public const string UseTransactions = baseName + nameof(UseTransactions);
    }
}