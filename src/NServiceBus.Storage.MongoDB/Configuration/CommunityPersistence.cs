namespace NServiceBus
{
    using System;
    using Configuration.AdvancedExtensibility;
    using Storage.MongoDB;

    /// <summary>
    /// Contains compatibility configurations for MongoDB community persistence packages.
    /// </summary>
    public static class CommunityPersistence
    {
        /// <summary>
        /// Configures compatibility for the NServiceBus.MongoDB community persistence package.
        /// </summary>
        public static MongoSettingsExtensions.CompatibilityConfiguration NServiceBusMongoDBCompatibility = configuration =>
        {
            var settings = configuration.GetSettings();
            settings.Set(SettingsKeys.VersionElementName, "DocumentVersion");
            settings.Set(SettingsKeys.CollectionNamingConvention, (Func<Type, string>)(type => type.Name));
        };

        /// <summary>
        /// Configures compatibility for the NServiceBus.Persistence.MongoDB community persistence package.
        /// </summary>
        public static MongoSettingsExtensions.CompatibilityConfiguration NServiceBusPersistenceMongoDBCompatibility(string versionElementName) => configuration =>
        {
            Guard.AgainstNullAndEmpty(nameof(versionElementName), versionElementName);

            configuration.GetSettings().Set(SettingsKeys.VersionElementName, versionElementName);
        };
    }
}