namespace NServiceBus.Storage.MongoDB;

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Extensibility;
using global::MongoDB.Driver;
using Outbox;

class OutboxPersister : IOutboxStorage
{
    public OutboxPersister(IMongoClient client, string partitionKey, bool readFallbackEnabled, string databaseName, MongoDatabaseSettings databaseSettings, Func<Type, string> collectionNamingConvention, MongoCollectionSettings collectionSettings)
    {
        outboxTransactionFactory = new OutboxTransactionFactory(client, databaseName, databaseSettings, collectionNamingConvention, MongoPersistence.DefaultTransactionTimeout);

        outboxRecordCollection = client.GetDatabase(databaseName, databaseSettings)
            .GetCollection<OutboxRecord>(collectionNamingConvention(typeof(OutboxRecord)), collectionSettings);

        this.partitionKey = partitionKey;
        this.readFallbackEnabled = readFallbackEnabled;
    }

    public async Task<OutboxMessage> Get(string messageId, ContextBag context,
        CancellationToken cancellationToken = default)
    {
        var outboxRecordId = CreateOutboxRecordId(messageId);

        // find by the structured ID first
        var outboxRecord = await outboxRecordCollection.Find(ByOutboxRecordId(outboxRecordId))
            .SingleOrDefaultAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

        if (outboxRecord is null && readFallbackEnabled)
        {
            // fallback to the legacy ID if the record wasn't found by the structured ID
            outboxRecord = await outboxRecordCollection.Find(ByMessageId(outboxRecordId.MessageId))
                .SingleOrDefaultAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        return outboxRecord != null
            ? new OutboxMessage(outboxRecordId.MessageId,
                outboxRecord.TransportOperations?.Select(op => op.ToTransportType()).ToArray())
            : null!;
    }

    OutboxRecordId CreateOutboxRecordId(string messageId) => new() { MessageId = messageId, PartitionKey = partitionKey };

    public Task<IOutboxTransaction>
        BeginTransaction(ContextBag context, CancellationToken cancellationToken = default) =>
        outboxTransactionFactory.BeginTransaction(context, cancellationToken);

    public Task Store(OutboxMessage message, IOutboxTransaction transaction, ContextBag context,
        CancellationToken cancellationToken = default)
    {
        var mongoOutboxTransaction = (OutboxTransaction)transaction;
        var storageSession = mongoOutboxTransaction.StorageSession;
        var storageTransportOperations =
            message.TransportOperations.Select(op => new StorageTransportOperation(op)).ToArray();

        var outboxRecordId = CreateOutboxRecordId(message.MessageId);

        return storageSession.InsertOneAsync(
            new OutboxRecord { Id = outboxRecordId, TransportOperations = storageTransportOperations },
            cancellationToken);
    }

    public async Task SetAsDispatched(string messageId, ContextBag context,
        CancellationToken cancellationToken = default)
    {
        var update = Builders<OutboxRecord>.Update
            .Set(record => record.TransportOperations, [])
            .CurrentDate(record => record.Dispatched);

        var outboxRecordId = CreateOutboxRecordId(messageId);

        // find by the structured ID first
        var updateResult = await outboxRecordCollection
            .UpdateOneAsync(ByOutboxRecordId(outboxRecordId), update, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        // This is safe to access because we assume the default collection and database settings are currently not exposed
        // and therefore WriteConcern.WMajority is always used which means IsAcknowledged is always true.
        // This is a safe assumption because not only are they not exposed they are also a good choice for the outbox semantics
        // as of now and changing it would require more careful consideration.
        if (updateResult.MatchedCount == 0 && readFallbackEnabled)
        {
            // fallback to the legacy ID if the record wasn't found by the structured ID
            await outboxRecordCollection
                .UpdateOneAsync(ByMessageId(outboxRecordId.MessageId), update, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }
    }

    static FilterDefinition<OutboxRecord> ByMessageId(string messageId) => Builders<OutboxRecord>.Filter.Eq("_id", messageId);

    static FilterDefinition<OutboxRecord> ByOutboxRecordId(OutboxRecordId outboxRecordId) => Builders<OutboxRecord>.Filter.Eq(r => r.Id, outboxRecordId);

    readonly OutboxTransactionFactory outboxTransactionFactory;
    readonly IMongoCollection<OutboxRecord> outboxRecordCollection;
    readonly string partitionKey;
    readonly bool readFallbackEnabled;
}