namespace NServiceBus.Storage.MongoDB;

using System;
using System.Threading;
using System.Threading.Tasks;
using Features;
using global::MongoDB.Driver;
using Installation;
using Microsoft.Extensions.DependencyInjection;
using Settings;

sealed class SubscriptionInstaller(IReadOnlySettings settings, IServiceProvider serviceProvider) : INeedToInstallSomething
{
    public async Task Install(string identity, CancellationToken cancellationToken = default)
    {
        var installerSettings = settings.GetOrDefault<InstallerSettings>();

        if (installerSettings is null || installerSettings.Disabled || !settings.IsFeatureActive<SubscriptionStorage>())
        {
            return;
        }

        var databaseName = settings.Get<string>(SettingsKeys.DatabaseName);
        var databaseSettings = settings.Get<MongoDatabaseSettings>();
        var collectionSettings = settings.Get<MongoCollectionSettings>();
        var collectionNamingConvention = settings.Get<Func<Type, string>>(SettingsKeys.CollectionNamingConvention);

        // We have to resolve the client provider here because at the time of the creation of the installer the provider might not be registered yet.
        var clientProvider = serviceProvider.GetRequiredService<IMongoClientProvider>();

        await CreateInfrastructureForSubscriptionTypes(clientProvider.Client, databaseSettings, databaseName, collectionSettings, collectionNamingConvention, cancellationToken)
            .ConfigureAwait(false);
    }

    internal static async Task CreateInfrastructureForSubscriptionTypes(IMongoClient client, MongoDatabaseSettings databaseSettings,
        string databaseName, MongoCollectionSettings collectionSettings, Func<Type, string> collectionNamingConvention, CancellationToken cancellationToken = default)
    {
        var collectionName = collectionNamingConvention(typeof(EventSubscription));
        var database = client.GetDatabase(databaseName, databaseSettings);

        await database.SafeCreateCollection(collectionName, cancellationToken).ConfigureAwait(false);

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