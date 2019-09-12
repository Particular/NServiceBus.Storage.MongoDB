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
            var collection = client.GetDatabase(databaseName).GetCollection<EventSubscription>(SubscriptionCollectionName);

            var indexKeyDefintion = Builders<EventSubscription>.IndexKeys
                .Ascending(x => x.MessageTypeName)
                .Ascending(x => x.TransportAddress)
                .Ascending(x => x.Endpoint); // allow queries to return results directly from the index
            var index = new CreateIndexModel<EventSubscription>(indexKeyDefintion, new CreateIndexOptions {Unique = true});
            collection.Indexes.CreateOne(index);

            context.Container.RegisterSingleton(new SubscriptionPersister(collection));
        }
    }
}