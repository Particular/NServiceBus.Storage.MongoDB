namespace NServiceBus.Storage.MongoDB
{
    using Features;

    class SagaStorage : Feature
    {
        SagaStorage()
        {
            DependsOn<Sagas>();
            DependsOn<SynchronizedStorageFeature>();
        }

        protected override void Setup(FeatureConfigurationContext context)
        {
            if (!context.Settings.TryGet(SettingsKeys.VersionFieldName, out string versionFieldName))
            {
                versionFieldName = "_version";
            }

            context.Container.ConfigureComponent(() => new SagaPersister(versionFieldName), DependencyLifecycle.SingleInstance);
        }
    }
}