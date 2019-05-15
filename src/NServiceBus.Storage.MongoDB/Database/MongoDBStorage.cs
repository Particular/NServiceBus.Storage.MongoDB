namespace NServiceBus.Storage.MongoDB
{
    using NServiceBus.Features;

    class MongoDBStorage : Feature
    {
        MongoDBStorage()
        {
        }

        /// <summary>
        /// Called when the feature should perform its initialization. This call will only happen if the feature is enabled.
        /// </summary>
        protected override void Setup(FeatureConfigurationContext context)
        {
            if (context.Settings.HasSetting(SettingsKeys.ConnectionStringName))
            {
                context.Container.MongoDBPersistence(context.Settings.Get<string>(SettingsKeys.ConnectionStringName));
            }

            else if (context.Settings.HasSetting(SettingsKeys.ConnectionString))
            {
                context.Container.MongoDBPersistence(() => context.Settings.Get<string>(SettingsKeys.ConnectionString));
            }
            else
            {
                context.Container.MongoDBPersistence();
            }
        }
    }
}