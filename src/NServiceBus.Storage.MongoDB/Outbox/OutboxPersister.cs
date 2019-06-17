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
            this.client = client;
            this.databaseName = databaseName;
            this.collectionNamingConvention = collectionNamingConvention;

            outboxRecordCollection = client.GetDatabase(databaseName).GetCollection<OutboxRecord>(collectionNamingConvention(typeof(OutboxRecord)));
        }

        public async Task<OutboxMessage> Get(string messageId, ContextBag context)
        {
            var record = await outboxRecordCollection.Find(filter => filter.Id == messageId).SingleOrDefaultAsync().ConfigureAwait(false);

            return record != null ? new OutboxMessage(record.Id, record.TransportOperations) : null;
        }

        public async Task<OutboxTransaction> BeginTransaction(ContextBag context)
        {
            var mongoSession = await client.StartSessionAsync().ConfigureAwait(false);

            mongoSession.StartTransaction();

            return new MongoOutboxTransaction(mongoSession, databaseName, collectionNamingConvention, context);
        }

        public Task Store(OutboxMessage message, OutboxTransaction transaction, ContextBag context)
        {
            var mongoOutboxTransaction = (MongoOutboxTransaction)transaction;
            var collection = mongoOutboxTransaction.StorageSession.GetCollection<OutboxRecord>();

            return collection.InsertOneAsync(new OutboxRecord { Id = message.MessageId, TransportOperations = message.TransportOperations });
        }

        public async Task SetAsDispatched(string messageId, ContextBag context)
        {
            var updateBuilder = Builders<OutboxRecord>.Update;
            var update = updateBuilder.Set(field => field.TransportOperations, new TransportOperation[0]);

            await outboxRecordCollection.UpdateOneAsync(filter => filter.Id == messageId, update).ConfigureAwait(false);
        }

        readonly IMongoClient client;
        readonly string databaseName;
        readonly Func<Type, string> collectionNamingConvention;
        readonly IMongoCollection<OutboxRecord> outboxRecordCollection;
    }

    class OutboxRecord
    {
        public string Id { get; set; }

        public TransportOperation[] TransportOperations { get; set; }
    }
}
