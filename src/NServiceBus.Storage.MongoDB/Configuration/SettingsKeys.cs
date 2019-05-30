namespace NServiceBus.Storage.MongoDB
{
    static class SettingsKeys
    {
        const string baseName = "MongoDB.";
        public const string VersionFieldName = baseName + nameof(VersionFieldName);
        public const string CollectionNamingScheme = baseName + nameof(CollectionNamingScheme);
        public const string DatabaseName = baseName + nameof(DatabaseName);
        public const string Client = baseName + nameof(Client);
        public const string UseTransactions = baseName + nameof(UseTransactions);
    }
}