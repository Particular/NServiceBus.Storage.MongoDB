namespace NServiceBus.Storage.MongoDB
{
    using NServiceBus.Features;

    class MongoDbSagaStorage : Feature
    {
        MongoDbSagaStorage()
        {
            DependsOn<Sagas>();
            DependsOn<MongoDbStorage>();
        }

        /// <summary>
        /// Called when the feature should perform its initialization. This call will only happen if the feature is enabled.
        /// </summary>
        protected override void Setup(FeatureConfigurationContext context)
        {
            context.Container.ConfigureComponent<SagaPersister>(DependencyLifecycle.InstancePerCall);
            context.Container.ConfigureComponent<MongoDbSagaRepository>(DependencyLifecycle.SingleInstance);
        }
    }
}