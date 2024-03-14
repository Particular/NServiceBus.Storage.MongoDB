namespace NServiceBus
{
    using System;
    using Configuration.AdvancedExtensibility;
    using Settings;
    using Storage.MongoDB;

    /// <summary>
    /// Provides settings to configure compatibility with community MongoDB persistence packages.
    /// </summary>
    public class CompatibilitySettings : ExposeSettings
    {
        internal CompatibilitySettings(SettingsHolder settingsHolder) : base(settingsHolder)
        {
        }

        /// <summary>
        /// The version element name with MongoDB conventions applied
        /// </summary>
        public CompatibilitySettings VersionElementName(string versionElementName)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(versionElementName);

            this.GetSettings().Set(SettingsKeys.VersionElementName, versionElementName);
            return this;
        }

        /// <summary>
        /// Sets the convention for collection naming based on the data type
        /// </summary>
        public CompatibilitySettings CollectionNamingConvention(Func<Type, string> collectionNamingConvention)
        {
            ArgumentNullException.ThrowIfNull(collectionNamingConvention);

            this.GetSettings().Set(SettingsKeys.CollectionNamingConvention, collectionNamingConvention);
            return this;
        }
    }
}