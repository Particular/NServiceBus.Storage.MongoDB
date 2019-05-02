namespace NServiceBus.Persistence.MongoDB.DataBus
{
    using NServiceBus.Features;
    using NServiceBus.Persistence.MongoDB.Database;

    public class MongoDbDataBusPersistence : Feature
    {
        public MongoDbDataBusPersistence()
        {
            DependsOn<MongoDbStorage>();
            DependsOn<Features.DataBus>();
        }

        protected override void Setup(FeatureConfigurationContext context)
        {
            context.Container.ConfigureComponent<GridFsDataBus>(DependencyLifecycle.SingleInstance);
        }
    }
}