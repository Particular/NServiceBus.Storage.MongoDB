namespace NServiceBus.Storage.MongoDB.Subscriptions
{
    using System;
    using Features;
    using global::MongoDB.Driver;
    using MongoDB;

    class SubscriptionStorage : Feature
    {
        const string SubscriptionCollectionName = "EventSubscriptions";

        public SubscriptionStorage()
        {
            DependsOn<MessageDrivenSubscriptions>();
        }

        protected override void Setup(FeatureConfigurationContext context)
        {
            var client = context.Settings.Get<Func<IMongoClient>>(SettingsKeys.MongoClient)();
            var databaseName = context.Settings.Get<string>(SettingsKeys.DatabaseName);
            var collection = GetSubscriptionCollection(client, databaseName);

            var subscriptionPersister = new SubscriptionPersister(collection);
            subscriptionPersister.CreateIndexes();

            context.Container.RegisterSingleton(subscriptionPersister);
        }

        //TODO do we need to set write/read concerns for the collection?
        internal static IMongoCollection<EventSubscription> GetSubscriptionCollection(IMongoClient client, string databaseName) => client.GetDatabase(databaseName).GetCollection<EventSubscription>(SubscriptionCollectionName);
    }
}