namespace NServiceBus.Storage.MongoDB
{
    using System;
    using System.Threading.Tasks;
    using Extensibility;
    using global::MongoDB.Driver;
    using Outbox;

    class MongoOutboxTransaction : OutboxTransaction
    {
        public MongoOutboxTransaction(IClientSessionHandle mongoSession, string databaseName, ContextBag context, Func<Type, string> collectionNamingConvention, TimeSpan transactionTimeout)
        {
            StorageSession = new StorageSession(mongoSession, databaseName, context, collectionNamingConvention, false, true, transactionTimeout);
            StorageSession.StartTransaction();
        }

        public StorageSession StorageSession { get; }

        public Task Commit()
        {
            return StorageSession.CommitTransaction();
        }

        public void Dispose()
        {
            StorageSession.Dispose();
        }
    }
}