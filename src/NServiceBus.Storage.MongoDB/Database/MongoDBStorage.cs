namespace NServiceBus.Storage.MongoDB
{
    using NServiceBus.Features;

    class MongoDBStorage : Feature
    {
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