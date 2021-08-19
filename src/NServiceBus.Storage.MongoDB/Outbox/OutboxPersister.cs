namespace NServiceBus.Storage.MongoDB
{
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Extensibility;
    using global::MongoDB.Driver;
    using Outbox;

    class OutboxPersister : IOutboxStorage
    {
        public OutboxPersister(IMongoClient client, string databaseName, Func<Type, string> collectionNamingConvention)
        {
            outboxTransactionFactory = new MongoOutboxTransactionFactory(client, databaseName, collectionNamingConvention, MongoPersistence.DefaultTransactionTimeout);

            var collectionSettings = new MongoCollectionSettings
            {
                ReadConcern = ReadConcern.Majority,
                ReadPreference = ReadPreference.Primary,
                WriteConcern = WriteConcern.WMajority
            };

            outboxRecordCollection = client.GetDatabase(databaseName).GetCollection<OutboxRecord>(collectionNamingConvention(typeof(OutboxRecord)), collectionSettings);
        }

        public async Task<OutboxMessage> Get(string messageId, ContextBag context, CancellationToken cancellationToken = default)
        {
            var outboxRecord = await outboxRecordCollection.Find(record => record.Id == messageId).SingleOrDefaultAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

            return outboxRecord != null ? new OutboxMessage(outboxRecord.Id, outboxRecord.TransportOperations?.Select(op => op.ToTransportType()).ToArray()) : null;
        }

        public Task<IOutboxTransaction> BeginTransaction(ContextBag context, CancellationToken cancellationToken = default) => outboxTransactionFactory.BeginTransaction(context, cancellationToken);

        public Task Store(OutboxMessage message, IOutboxTransaction transaction, ContextBag context, CancellationToken cancellationToken = default)
        {
            var mongoOutboxTransaction = (MongoOutboxTransaction)transaction;
            var storageSession = mongoOutboxTransaction.StorageSession;
            var storageTransportOperations = message.TransportOperations.Select(op => new StorageTransportOperation(op)).ToArray();

            return storageSession.InsertOneAsync(new OutboxRecord { Id = message.MessageId, TransportOperations = storageTransportOperations }, cancellationToken);
        }

        public async Task SetAsDispatched(string messageId, ContextBag context, CancellationToken cancellationToken = default)
        {
            var update = Builders<OutboxRecord>.Update
                .Set(record => record.TransportOperations, Array.Empty<StorageTransportOperation>())
                .CurrentDate(record => record.Dispatched);

            await outboxRecordCollection.UpdateOneAsync(record => record.Id == messageId, update, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        readonly MongoOutboxTransactionFactory outboxTransactionFactory;
        readonly IMongoCollection<OutboxRecord> outboxRecordCollection;
    }
}