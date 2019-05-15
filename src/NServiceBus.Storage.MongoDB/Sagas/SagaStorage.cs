namespace NServiceBus.Storage.MongoDB
{
    using NServiceBus.Features;

    class SagaStorage : Feature
    {
        SagaStorage()
        {
            DependsOn<Sagas>();
            DependsOn<MongoDBStorage>();
        }

        /// <summary>
        /// Called when the feature should perform its initialization. This call will only happen if the feature is enabled.
        /// </summary>
        protected override void Setup(FeatureConfigurationContext context)
        {
            context.Container.ConfigureComponent<SagaPersister>(DependencyLifecycle.InstancePerCall);
            context.Container.ConfigureComponent<SagaRepository>(DependencyLifecycle.SingleInstance);
        }
    }
}