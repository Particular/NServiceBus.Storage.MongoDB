namespace NServiceBus.Storage.MongoDB
{
    using NServiceBus.Features;

    class MongoDbStorage : Feature
    {
        MongoDbStorage()
        {
        }

        /// <summary>
        /// Called when the feature should perform its initialization. This call will only happen if the feature is enabled.
        /// </summary>
        protected override void Setup(FeatureConfigurationContext context)
        {
            if (context.Settings.HasSetting(MongoPersistenceSettings.ConnectionStringName))
            {
                context.Container.MongoDbPersistence(context.Settings.Get<string>(MongoPersistenceSettings.ConnectionStringName));
            }

            else if (context.Settings.HasSetting(MongoPersistenceSettings.ConnectionString))
            {
                context.Container.MongoDbPersistence(() => context.Settings.Get<string>(MongoPersistenceSettings.ConnectionString));
            }
            else
            {
                context.Container.MongoDbPersistence();
            }
        }
    }
}