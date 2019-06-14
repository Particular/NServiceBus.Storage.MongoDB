using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Driver;
using NServiceBus.Logging;
using NServiceBus.Outbox;

namespace NServiceBus.Storage.MongoDB
{
    class MongoOutboxTransaction : OutboxTransaction
    {
        public MongoOutboxTransaction(IClientSessionHandle mongoSession, string databaseName)
        {
            this.mongoSession = mongoSession;
            this.databaseName = databaseName;
        }

        public IMongoCollection<OutboxRecord> GetCollection() => mongoSession.Client.GetDatabase(databaseName).GetCollection<OutboxRecord>("outbox");

        public Task Commit()
        {
            return mongoSession.CommitTransactionAsync();
        }

        public void Dispose()
        {
            try
            {
                mongoSession.AbortTransaction();
            }
            catch (Exception ex)
            {
                Log.Warn("Exception thrown while aborting transaction", ex);
            }

            mongoSession.Dispose();
        }

        static readonly ILog Log = LogManager.GetLogger<MongoOutboxTransaction>();

        readonly IClientSessionHandle mongoSession;
        readonly string databaseName;
    }
}
