using System;
using MongoDB.Driver;
using NServiceBus.Features;
using NServiceBus.Unicast.Subscriptions.MessageDrivenSubscriptions;

namespace NServiceBus.Storage.MongoDB
{
    class SubscriptionStorage : Feature
    {
        public SubscriptionStorage()
        {
            DependsOn<MessageDrivenSubscriptions>();
        }

        protected override void Setup(FeatureConfigurationContext context)
        {
            var client = context.Settings.Get<Func<IMongoClient>>(SettingsKeys.MongoClient)();
            var databaseName = context.Settings.Get<string>(SettingsKeys.DatabaseName);
            var databaseSettings = context.Settings.Get<MongoDatabaseSettings>();
            var collectionNamingConvention = context.Settings.Get<Func<Type, string>>(SettingsKeys.CollectionNamingConvention);
            var subscriptionCollectionName = collectionNamingConvention(typeof(EventSubscription));
            var collection = client.GetDatabase(databaseName, databaseSettings).GetCollection<EventSubscription>(subscriptionCollectionName);

            var subscriptionPersister = new SubscriptionPersister(collection);
            subscriptionPersister.CreateIndexes();

            context.Container.RegisterSingleton<ISubscriptionStorage>(subscriptionPersister);
        }
    }
}