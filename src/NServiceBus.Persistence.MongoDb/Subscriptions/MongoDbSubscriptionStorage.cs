namespace NServiceBus.Persistence.MongoDB.Subscriptions
{
    using NServiceBus.Features;
    using NServiceBus.Persistence.MongoDB.Database;

    public class MongoDbSubscriptionStorage : Feature
    {
        internal MongoDbSubscriptionStorage()
        {
            DependsOn<MessageDrivenSubscriptions>();
            DependsOn<MongoDbStorage>();
        }

        /// <summary>
        /// Called when the feature should perform its initialization. This call will only happen if the feature is enabled.
        /// </summary>
        protected override void Setup(FeatureConfigurationContext context)
        {
            context.Container.ConfigureComponent<SubscriptionPersister>(DependencyLifecycle.InstancePerCall);
        }
    }
}