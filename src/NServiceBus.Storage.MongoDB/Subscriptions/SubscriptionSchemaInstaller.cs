namespace NServiceBus.Storage.MongoDB;

using System;
using System.Threading;
using System.Threading.Tasks;
using global::MongoDB.Driver;
using Installation;
using Settings;

sealed class SubscriptionSchemaInstaller(IReadOnlySettings settings, InstallerSettings installerSettings) : INeedToInstallSomething
{
    public Task Install(string identity, CancellationToken cancellationToken = default)
    {
        if (installerSettings.Disabled || !settings.TryGet<Func<IMongoClient>>(SettingsKeys.MongoClient, out Func<IMongoClient>? client))
        {
            return Task.CompletedTask;
        }

        var databaseName = settings.Get<string>(SettingsKeys.DatabaseName);
        var databaseSettings = settings.Get<MongoDatabaseSettings>();
        var collectionSettings = settings.Get<MongoCollectionSettings>();
        var collectionNamingConvention = settings.Get<Func<Type, string>>(SettingsKeys.CollectionNamingConvention);

        InitializeSubscription(client(), databaseSettings, databaseName, collectionSettings, collectionNamingConvention);

        return Task.CompletedTask;
    }

    internal static void InitializeSubscription(IMongoClient client, MongoDatabaseSettings databaseSettings,
        string databaseName, MongoCollectionSettings collectionSettings, Func<Type, string> collectionNamingConvention)
    {
        var subscriptionCollectionName = collectionNamingConvention(typeof(EventSubscription));
        var collection = client.GetDatabase(databaseName, databaseSettings).GetCollection<EventSubscription>(subscriptionCollectionName, collectionSettings);
        var uniqueIndex = new CreateIndexModel<EventSubscription>(Builders<EventSubscription>.IndexKeys
                .Ascending(x => x.MessageTypeName)
                .Ascending(x => x.TransportAddress),
            new CreateIndexOptions { Unique = true });
        var searchIndex = new CreateIndexModel<EventSubscription>(Builders<EventSubscription>.IndexKeys
            .Ascending(x => x.MessageTypeName)
            .Ascending(x => x.TransportAddress)
            .Ascending(x => x.Endpoint));
        collection.Indexes.CreateMany([uniqueIndex, searchIndex]);
    }
}