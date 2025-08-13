namespace NServiceBus.Storage.MongoDB;

using System;
using System.Threading;
using System.Threading.Tasks;
using global::MongoDB.Driver;
using Installation;
using Settings;

sealed class SubscriptionInstaller(IReadOnlySettings settings) : INeedToInstallSomething
{
    public async Task Install(string identity, CancellationToken cancellationToken = default)
    {
        var installerSettings = settings.Get<InstallerSettings>();
        if (installerSettings.Disabled || !settings.TryGet<Func<IMongoClient>>(SettingsKeys.MongoClient, out Func<IMongoClient>? client))
        {
            return;
        }

        var databaseName = settings.Get<string>(SettingsKeys.DatabaseName);
        var databaseSettings = settings.Get<MongoDatabaseSettings>();
        var collectionSettings = settings.Get<MongoCollectionSettings>();
        var collectionNamingConvention = settings.Get<Func<Type, string>>(SettingsKeys.CollectionNamingConvention);

        await CreateInfrastructureForSubscriptionTypes(client(), databaseSettings, databaseName, collectionSettings, collectionNamingConvention, cancellationToken)
            .ConfigureAwait(false);
    }

    internal static async Task CreateInfrastructureForSubscriptionTypes(IMongoClient client, MongoDatabaseSettings databaseSettings,
        string databaseName, MongoCollectionSettings collectionSettings, Func<Type, string> collectionNamingConvention, CancellationToken cancellationToken = default)
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
        await collection.Indexes.CreateManyAsync([uniqueIndex, searchIndex], cancellationToken)
            .ConfigureAwait(false);
    }
}