namespace NServiceBus.Storage.MongoDB
{
    using System;
    using Features;
    using global::MongoDB.Driver;
    using Microsoft.Extensions.DependencyInjection;
    using Unicast.Subscriptions.MessageDrivenSubscriptions;

    class SubscriptionStorage : Feature
    {
        public SubscriptionStorage()
        {
            DependsOn("NServiceBus.Features.MessageDrivenSubscriptions");
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

            context.Services.AddSingleton<ISubscriptionStorage>(subscriptionPersister);
        }
    }
}