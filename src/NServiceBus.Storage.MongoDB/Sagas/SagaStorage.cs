namespace NServiceBus.Storage.MongoDB
{
    using NServiceBus.Features;

    class SagaStorage : Feature
    {
        SagaStorage()
        {
            DependsOn<Sagas>();
        }

        protected override void Setup(FeatureConfigurationContext context)
        {
            context.Container.ConfigureComponent<SagaPersister>(DependencyLifecycle.InstancePerCall);
            context.Container.ConfigureComponent<SagaRepository>(DependencyLifecycle.SingleInstance);
        }
    }
}