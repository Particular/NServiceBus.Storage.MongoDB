using NServiceBus.Features;

namespace NServiceBus.Storage.MongoDB
{
    class SagaStorage : Feature
    {
        SagaStorage()
        {
            DependsOn<Features.Sagas>();
            DependsOn<SynchronizedStorage>();
        }

        protected override void Setup(FeatureConfigurationContext context)
        {
            if (!context.Settings.TryGet(SettingsKeys.VersionElementName, out string versionElementName))
            {
                versionElementName = "_version";
            }

            context.Container.ConfigureComponent(() => new SagaPersister(versionElementName), DependencyLifecycle.SingleInstance);
        }
    }
}