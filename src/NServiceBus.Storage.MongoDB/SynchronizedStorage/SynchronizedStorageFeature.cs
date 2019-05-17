using NServiceBus.Features;

namespace NServiceBus.Storage.MongoDB
{
    class SynchronizedStorageFeature : Feature
    {
        public SynchronizedStorageFeature()
        {
            DependsOn<ConfigureMongoDBPersistence>();
        }

        protected override void Setup(FeatureConfigurationContext context)
        {
            context.Container.ConfigureComponent<SynchronizedStorage>(DependencyLifecycle.SingleInstance);
        }
    }
}
