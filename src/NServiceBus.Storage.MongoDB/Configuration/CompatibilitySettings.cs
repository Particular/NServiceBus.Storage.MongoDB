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
        /// The version element name with MongoDB conventions applied
        /// </summary>
        /// <param name="versionElementName"></param>
        /// <returns></returns>
        public CompatibilitySettings VersionElementName(string versionElementName)
        {
            Guard.AgainstNullAndEmpty(nameof(versionElementName), versionElementName);

            this.GetSettings().Set(SettingsKeys.VersionElementName, versionElementName);
            return this;
        }

        /// <summary>
        /// Sets the convention for saga collection naming based on the saga data type
        /// </summary>
        /// <param name="collectionNamingConvention"></param>
        /// <returns></returns>
        public CompatibilitySettings CollectionNamingConvention(Func<Type, string> collectionNamingConvention)
        {
            Guard.AgainstNull(nameof(collectionNamingConvention), collectionNamingConvention);

            //TODO: make sure null isn't returned or throw with collectionNamingConvention

            this.GetSettings().Set(SettingsKeys.CollectionNamingConvention, collectionNamingConvention);
            return this;
        }
    }
}