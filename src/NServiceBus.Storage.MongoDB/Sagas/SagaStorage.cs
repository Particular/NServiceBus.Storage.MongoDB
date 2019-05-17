namespace NServiceBus.Storage.MongoDB
{
    using NServiceBus.Features;

    class SagaStorage : Feature
    {
        SagaStorage()
        {
            DependsOn<Sagas>();
            DependsOn<SynchronizedStorageFeature>();
        }

        protected override void Setup(FeatureConfigurationContext context)
        {
            context.Container.ConfigureComponent<SagaPersister>(DependencyLifecycle.SingleInstance);
        }
    }
}