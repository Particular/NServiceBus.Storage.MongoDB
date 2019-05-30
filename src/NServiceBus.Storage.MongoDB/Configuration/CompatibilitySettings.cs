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

        public CompatibilitySettings CollectionNamingConvention(Func<Type, string> collectionNamingConvention)
        {
            Guard.AgainstNull(nameof(collectionNamingConvention), collectionNamingConvention);

            //TODO: make sure null isn't returned or throw with collectionNamingConvention

            this.GetSettings().Set(SettingsKeys.CollectionNamingConvention, collectionNamingConvention);
            return this;
        }
    }
}