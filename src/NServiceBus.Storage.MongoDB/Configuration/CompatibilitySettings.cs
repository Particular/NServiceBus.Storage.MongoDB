using System;
using NServiceBus.Configuration.AdvancedExtensibility;
using NServiceBus.Settings;
using NServiceBus.Storage.MongoDB;

namespace NServiceBus
{
    public class CompatibilitySettings : ExposeSettings
    {
        internal CompatibilitySettings(SettingsHolder settingsHolder) : base(settingsHolder) { }

        /// <summary>
        /// The version field name with MongoDB conventions applied
        /// </summary>
        /// <param name="versionFieldName"></param>
        /// <returns></returns>
        public CompatibilitySettings VersionFieldName(string versionFieldName)
        {
            Guard.AgainstNullAndEmpty(nameof(versionFieldName), versionFieldName);

            this.GetSettings().Set(SettingsKeys.VersionFieldName, versionFieldName);
            return this;
        }

        public CompatibilitySettings CollectionNamingScheme(Func<Type, string> collectionNamingScheme)
        {
            Guard.AgainstNull(nameof(collectionNamingScheme), collectionNamingScheme);

            //TODO: make sure null isn't returned or throw with collectionNamingScheme

            this.GetSettings().Set(SettingsKeys.CollectionNamingScheme, collectionNamingScheme);
            return this;
        }
    }
}