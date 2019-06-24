using System;
using System.Threading.Tasks;
using MongoDB.Driver;
using NServiceBus.Extensibility;
using NServiceBus.Outbox;

namespace NServiceBus.Storage.MongoDB
{
    class OutboxPersister : IOutboxStorage
    {
        public OutboxPersister(IMongoClient client, string databaseName, Func<Type, string> collectionNamingConvention)
        {
            outboxTransactionFactory = new MongoOutboxTransactionFactory(client, databaseName, collectionNamingConvention);
            outboxRecordCollection = client.GetDatabase(databaseName).GetCollection<OutboxRecord>(collectionNamingConvention(typeof(OutboxRecord)));
        }

        public async Task<OutboxMessage> Get(string messageId, ContextBag context)
        {
            var outboxRecord = await outboxRecordCollection.Find(record => record.Id == messageId).SingleOrDefaultAsync().ConfigureAwait(false);

            return outboxRecord != null ? new OutboxMessage(outboxRecord.Id, outboxRecord.TransportOperations) : null;
        }

        public Task<OutboxTransaction> BeginTransaction(ContextBag context) => outboxTransactionFactory.BeginTransaction(context);

        public Task Store(OutboxMessage message, OutboxTransaction transaction, ContextBag context)
        {
            var mongoOutboxTransaction = (MongoOutboxTransaction)transaction;
            var collection = mongoOutboxTransaction.StorageSession.GetCollection<OutboxRecord>();

            return collection.InsertOneAsync(new OutboxRecord { Id = message.MessageId, TransportOperations = message.TransportOperations });
        }

        public async Task SetAsDispatched(string messageId, ContextBag context)
        {
            var update = Builders<OutboxRecord>.Update
                .Set(record => record.TransportOperations, new TransportOperation[0])
                .CurrentDate(record => record.Dispatched);

            await outboxRecordCollection.UpdateOneAsync(record => record.Id == messageId, update).ConfigureAwait(false);
        }

        readonly MongoOutboxTransactionFactory outboxTransactionFactory;
        readonly IMongoCollection<OutboxRecord> outboxRecordCollection;
    }
}
