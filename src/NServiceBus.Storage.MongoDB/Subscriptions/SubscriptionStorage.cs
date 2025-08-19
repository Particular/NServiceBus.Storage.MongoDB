namespace NServiceBus.Storage.MongoDB;

using System;
using Features;
using global::MongoDB.Driver;
using Microsoft.Extensions.DependencyInjection;
using Unicast.Subscriptions.MessageDrivenSubscriptions;

class SubscriptionStorage : Feature
{
    public SubscriptionStorage() => DependsOn("NServiceBus.Features.MessageDrivenSubscriptions");

    protected override void Setup(FeatureConfigurationContext context)
    {
        var databaseName = context.Settings.Get<string>(SettingsKeys.DatabaseName);
        var collectionNamingConvention = context.Settings.Get<Func<Type, string>>(SettingsKeys.CollectionNamingConvention);
        var databaseSettings = context.Settings.Get<MongoDatabaseSettings>();
        var collectionSettings = context.Settings.Get<MongoCollectionSettings>();

        context.Services.AddSingleton<ISubscriptionStorage>(sp => new SubscriptionPersister(sp.GetRequiredService<IMongoClientProvider>().Client, databaseName, databaseSettings, collectionNamingConvention, collectionSettings));
    }
}