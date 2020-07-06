namespace NServiceBus.Storage.MongoDB
{
    using System;
    using System.Threading.Tasks;
    using Extensibility;
    using global::MongoDB.Driver;
    using Persistence;

    class StorageSessionFactory : ISynchronizedStorage
    {
        public StorageSessionFactory(IMongoClient client, bool useTransactions, bool useOptimisticConcurrency, string databaseName, Func<Type, string> collectionNamingConvention, TimeSpan transactionTimeout)
        {
            this.client = client;
            this.useTransactions = useTransactions;
            this.useOptimisticConcurrency = useOptimisticConcurrency;
            this.databaseName = databaseName;
            this.collectionNamingConvention = collectionNamingConvention;
            this.transactionTimeout = transactionTimeout;
        }

        public async Task<CompletableSynchronizedStorageSession> OpenSession(ContextBag contextBag)
        {
            var mongoSession = await client.StartSessionAsync().ConfigureAwait(false);

            var session = new StorageSession(mongoSession, databaseName, contextBag, collectionNamingConvention, true, useTransactions, useOptimisticConcurrency, transactionTimeout);
            session.StartTransaction();
            return session;
        }

        readonly IMongoClient client;
        readonly bool useTransactions;
        readonly bool useOptimisticConcurrency;
        readonly string databaseName;
        readonly Func<Type, string> collectionNamingConvention;
        readonly TimeSpan transactionTimeout;
    }
}