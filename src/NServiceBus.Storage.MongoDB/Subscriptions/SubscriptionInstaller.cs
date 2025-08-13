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
        var collectionName = collectionNamingConvention(typeof(EventSubscription));
        var database = client.GetDatabase(databaseName, databaseSettings);

        try
        {
            await database.CreateCollectionAsync(collectionName, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }
        catch (MongoCommandException ex) when (ex is { Code: 48, CodeName: "NamespaceExists" })
        {
            //Collection already exists, so swallow the exception
        }

        var collection = database.GetCollection<EventSubscription>(collectionName, collectionSettings);
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