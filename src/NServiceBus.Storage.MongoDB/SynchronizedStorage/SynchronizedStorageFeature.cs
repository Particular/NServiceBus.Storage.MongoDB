using NServiceBus.Features;

namespace NServiceBus.Storage.MongoDB
{
    class SynchronizedStorageFeature : Feature
    {
        protected override void Setup(FeatureConfigurationContext context)
        {
            context.Settings.TryGet(SettingsKeys.ConnectionString, out string connectionString);

            context.Container.MongoDBPersistence(() => connectionString);

            context.Container.ConfigureComponent<SynchronizedStorage>(DependencyLifecycle.SingleInstance);
        }
    }
}
