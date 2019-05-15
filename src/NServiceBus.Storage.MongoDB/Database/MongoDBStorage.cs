namespace NServiceBus.Storage.MongoDB
{
    using NServiceBus.Features;

    class MongoDBStorage : Feature
    {
        protected override void Setup(FeatureConfigurationContext context)
        {
            context.Settings.TryGet(SettingsKeys.ConnectionString, out string connectionString);

            context.Container.MongoDBPersistence(() => connectionString);
        }
    }
}