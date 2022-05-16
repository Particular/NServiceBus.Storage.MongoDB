namespace NServiceBus.Storage.MongoDB
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Extensibility;
    using global::MongoDB.Driver;

    class StorageSessionFactory
    {
        public StorageSessionFactory(IMongoClient client, bool useTransactions, string databaseName, Func<Type, string> collectionNamingConvention, TimeSpan transactionTimeout)
        {
            this.client = client;
            this.useTransactions = useTransactions;
            this.databaseName = databaseName;
            this.collectionNamingConvention = collectionNamingConvention;
            this.transactionTimeout = transactionTimeout;
        }

        public async Task<StorageSession> OpenSession(ContextBag contextBag, CancellationToken cancellationToken = default)
        {
            var mongoSession = await client.StartSessionAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

            var session = new StorageSession(mongoSession, databaseName, contextBag, collectionNamingConvention, useTransactions, transactionTimeout);
            session.StartTransaction();
            return session;
        }

        readonly IMongoClient client;
        readonly bool useTransactions;
        readonly string databaseName;
        readonly Func<Type, string> collectionNamingConvention;
        readonly TimeSpan transactionTimeout;
    }
}