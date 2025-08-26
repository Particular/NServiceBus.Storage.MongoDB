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
        var outboxRecordId = new OutboxRecordId { MessageId = messageId, PartitionKey = partitionKey };

        var equalityPredicateWithFallback = CreateOutboxRecordFilterPredicate(outboxRecordId);

        var outboxRecord = await outboxRecordCollection.Find(equalityPredicateWithFallback)
            .SingleOrDefaultAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

        return outboxRecord != null
            ? new OutboxMessage(outboxRecordId.MessageId,
                outboxRecord.TransportOperations?.Select(op => op.ToTransportType()).ToArray())
            : null!;
    }

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

        var outboxRecordId = new OutboxRecordId { MessageId = message.MessageId, PartitionKey = partitionKey };

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

        var outboxRecordId = new OutboxRecordId { MessageId = messageId, PartitionKey = partitionKey };

        var equalityPredicateWithFallback = CreateOutboxRecordFilterPredicate(outboxRecordId);

        await outboxRecordCollection
            .UpdateOneAsync(equalityPredicateWithFallback, update, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    FilterDefinition<OutboxRecord> CreateOutboxRecordFilterPredicate(OutboxRecordId outboxRecordId)
    {
        var isStructuredId = Builders<OutboxRecord>.Filter.Eq(r => r.Id, outboxRecordId);
        return readFallbackEnabled
            ? Builders<OutboxRecord>.Filter.Or(isStructuredId, Builders<OutboxRecord>.Filter.Eq("_id", outboxRecordId.MessageId))
            : isStructuredId;
    }

    readonly OutboxTransactionFactory outboxTransactionFactory;
    readonly IMongoCollection<OutboxRecord> outboxRecordCollection;
    readonly string partitionKey;
    readonly bool readFallbackEnabled;
}