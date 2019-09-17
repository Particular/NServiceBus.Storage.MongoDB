namespace NServiceBus.Storage.MongoDB.Subscriptions
{
    using System;
    using Features;
    using global::MongoDB.Driver;
    using MongoDB;

    class SubscriptionStorage : Feature
    {
        const string SubscriptionCollectionName = "eventsubscriptions";

        public SubscriptionStorage()
        {
            DependsOn<MessageDrivenSubscriptions>();
        }

        protected override void Setup(FeatureConfigurationContext context)
        {
            var client = context.Settings.Get<Func<IMongoClient>>(SettingsKeys.MongoClient)();
            var databaseName = context.Settings.Get<string>(SettingsKeys.DatabaseName);
            var databaseSettings = context.Settings.Get<MongoDatabaseSettings>();
            var collection = GetSubscriptionCollection(client, databaseName, databaseSettings);

            var subscriptionPersister = new SubscriptionPersister(collection);
            subscriptionPersister.CreateIndexes();

            context.Container.RegisterSingleton(subscriptionPersister);
        }

        internal static IMongoCollection<EventSubscription> GetSubscriptionCollection(IMongoClient client, string databaseName, MongoDatabaseSettings databaseSettings) =>
            client.GetDatabase(databaseName, databaseSettings).GetCollection<EventSubscription>(SubscriptionCollectionName);
    }
}