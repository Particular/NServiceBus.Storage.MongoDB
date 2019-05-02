namespace NServiceBus.Persistence.MongoDB.Gateway
{
    using NServiceBus.Features;
    using NServiceBus.Persistence.MongoDB.Database;

    public class MongoDbGatewayDeduplication : Feature
    {
        public MongoDbGatewayDeduplication()
        {
            DependsOn("Gateway");
            DependsOn<MongoDbStorage>();
        }

        protected override void Setup(FeatureConfigurationContext context)
        {
            context.Container.ConfigureComponent<Deduplication>(DependencyLifecycle.InstancePerCall);
        }
    }
}