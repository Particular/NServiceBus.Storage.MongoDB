namespace NServiceBus.Storage.MongoDB
{
    using Features;
    using global::MongoDB.Bson.Serialization.Conventions;

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

            var pack = new ConventionPack();
            pack.Add(new IgnoreExtraElementsConvention(true));
            ConventionRegistry.Register("Ignore Extra Saga Data Elements", pack, t => t.IsAssignableFrom(typeof(IContainSagaData)));

            context.Container.ConfigureComponent(() => new SagaPersister(versionFieldName), DependencyLifecycle.SingleInstance);
        }
    }
}