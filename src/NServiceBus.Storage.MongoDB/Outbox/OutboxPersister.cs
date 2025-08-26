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
    public OutboxPersister(IMongoClient client, string partitionKey, string databaseName, MongoDatabaseSettings databaseSettings, Func<Type, string> collectionNamingConvention, MongoCollectionSettings collectionSettings)
    {
        outboxTransactionFactory = new MongoOutboxTransactionFactory(client, databaseName, databaseSettings, collectionNamingConvention, MongoPersistence.DefaultTransactionTimeout);

        outboxRecordCollection = client.GetDatabase(databaseName, databaseSettings)
            .GetCollection<OutboxRecord>(collectionNamingConvention(typeof(OutboxRecord)), collectionSettings);

        this.partitionKey = partitionKey;
    }

    public async Task<OutboxMessage> Get(string messageId, ContextBag context,
        CancellationToken cancellationToken = default)
    {
        var outboxRecordId = new OutboxRecordId { MessageId = messageId, PartitionKey = partitionKey };
        var outboxRecord = await outboxRecordCollection.Find(record => record.Id == outboxRecordId)
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
        var mongoOutboxTransaction = (MongoOutboxTransaction)transaction;
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

        await outboxRecordCollection
            .UpdateOneAsync(record => record.Id == outboxRecordId, update, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    readonly MongoOutboxTransactionFactory outboxTransactionFactory;
    readonly IMongoCollection<OutboxRecord> outboxRecordCollection;
    readonly string partitionKey;
}