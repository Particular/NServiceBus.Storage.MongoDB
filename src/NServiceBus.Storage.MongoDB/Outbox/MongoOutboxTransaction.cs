using System;
using System.Threading.Tasks;
using MongoDB.Driver;
using NServiceBus.Extensibility;
using NServiceBus.Outbox;

namespace NServiceBus.Storage.MongoDB
{
    class MongoOutboxTransaction : OutboxTransaction
    {
        public StorageSession StorageSession { get; }

        public MongoOutboxTransaction(IClientSessionHandle mongoSession, string databaseName, Func<Type, string> collectionNamingConvention, ContextBag context)
        {
            StorageSession = new StorageSession(mongoSession, databaseName, context, collectionNamingConvention, false);
        }

        public Task Commit()
        {
            return StorageSession.InternalCompleteAsync();
        }

        public void Dispose()
        {
            StorageSession.InternalDispose();
        }
    }
}
