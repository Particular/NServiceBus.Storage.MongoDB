namespace NServiceBus.Storage.MongoDB;

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using global::MongoDB.Bson;
using global::MongoDB.Driver;
using Installation;
using Settings;

sealed class OutboxInstaller(IReadOnlySettings settings, IMongoClientProvider clientProvider) : INeedToInstallSomething
{
    public async Task Install(string identity, CancellationToken cancellationToken = default)
    {
        var databaseName = settings.Get<string>(SettingsKeys.DatabaseName);
        var databaseSettings = settings.Get<MongoDatabaseSettings>();
        var collectionSettings = settings.Get<MongoCollectionSettings>();
        var collectionNamingConvention = settings.Get<Func<Type, string>>(SettingsKeys.CollectionNamingConvention);
        var timeToKeepOutboxDeduplicationData = settings.Get<OutboxPersistenceConfiguration>().TimeToKeepDeduplicationData;

        await CreateInfrastructureForOutboxTypes(clientProvider.Client, databaseName, databaseSettings, collectionNamingConvention, collectionSettings, timeToKeepOutboxDeduplicationData, cancellationToken)
            .ConfigureAwait(false);
    }

    internal static async Task CreateInfrastructureForOutboxTypes(IMongoClient client, string databaseName, MongoDatabaseSettings databaseSettings, Func<Type, string> collectionNamingConvention, MongoCollectionSettings collectionSettings, TimeSpan timeToKeepOutboxDeduplicationData, CancellationToken cancellationToken = default)
    {
        var database = client.GetDatabase(databaseName, databaseSettings);
        var collectionName = collectionNamingConvention(typeof(OutboxRecord));

        await database.SafeCreateCollection(collectionName, cancellationToken).ConfigureAwait(false);

        var outboxCollection = database.GetCollection<OutboxRecord>(collectionName, collectionSettings);

        var outboxIndexesCursor = await outboxCollection.Indexes.ListAsync(cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        var outboxIndexes = await outboxIndexesCursor.ToListAsync(cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        var outboxCleanupIndex = outboxIndexes.SingleOrDefault(indexDocument => indexDocument.GetElement("name").Value == OutboxCleanupIndexName);
        var createIndex = false;

        if (outboxCleanupIndex is null)
        {
            createIndex = true;
        }
        else if (!outboxCleanupIndex.TryGetElement("expireAfterSeconds", out BsonElement existingExpiration) ||
                 TimeSpan.FromSeconds(existingExpiration.Value.ToInt32()) != timeToKeepOutboxDeduplicationData)
        {
            await outboxCollection.Indexes.DropOneAsync(OutboxCleanupIndexName, cancellationToken)
                .ConfigureAwait(false);
            createIndex = true;
        }

        if (!createIndex)
        {
            return;
        }

        var indexModel = new CreateIndexModel<OutboxRecord>(
            Builders<OutboxRecord>.IndexKeys.Ascending(record => record.Dispatched),
            new CreateIndexOptions
            {
                ExpireAfter = timeToKeepOutboxDeduplicationData,
                Name = OutboxCleanupIndexName,
                Background = true
            });

        await outboxCollection.Indexes.CreateOneAsync(indexModel, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    internal const string OutboxCleanupIndexName = "OutboxCleanup";
}