using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Driver;
using NServiceBus.Extensibility;
using NServiceBus.Outbox;

namespace NServiceBus.Storage.MongoDB
{
    class OutboxPersister : IOutboxStorage
    {
        public OutboxPersister(IMongoClient client, string databaseName)
        {
            this.client = client;
            this.databaseName = databaseName;
        }

        public async Task<OutboxMessage> Get(string messageId, ContextBag context)
        {
            var record = await client.GetDatabase(databaseName).GetCollection<OutboxRecord>("outbox").Find(filter => filter.Id == messageId).SingleOrDefaultAsync().ConfigureAwait(false);

            return record?.OutboxMessage;
        }

        public async Task<OutboxTransaction> BeginTransaction(ContextBag context)
        {
            var mongoSession = await client.StartSessionAsync().ConfigureAwait(false);

            mongoSession.StartTransaction();

            return new MongoOutboxTransaction(mongoSession, databaseName);
        }

        public Task Store(OutboxMessage message, OutboxTransaction transaction, ContextBag context)
        {
            var mongoOutboxTransaction = (MongoOutboxTransaction)transaction;
            var collection = mongoOutboxTransaction.GetCollection();

            return collection.InsertOneAsync(new OutboxRecord { Id = message.MessageId, OutboxMessage = message });
        }

        public async Task SetAsDispatched(string messageId, ContextBag context)
        {
            var collection = client.GetDatabase(databaseName).GetCollection<OutboxRecord>("outbox");

            var updateBuilder = Builders<OutboxRecord>.Update;
            var update = updateBuilder.Set(field => field.OutboxMessage.TransportOperations, new TransportOperation[0]);

            await collection.UpdateOneAsync(filter => filter.Id == messageId, update).ConfigureAwait(false);
        }

        readonly IMongoClient client;
        readonly string databaseName;
    }

    class OutboxRecord
    {
        public string Id { get; set; }

        public OutboxMessage OutboxMessage { get; set; }
    }
}
