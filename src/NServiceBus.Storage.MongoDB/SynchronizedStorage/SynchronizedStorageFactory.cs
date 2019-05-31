using System;
using System.Threading.Tasks;
using MongoDB.Driver;
using NServiceBus.Extensibility;
using NServiceBus.Persistence;

namespace NServiceBus.Storage.MongoDB
{
    class SynchronizedStorageFactory : ISynchronizedStorage
    {
        public SynchronizedStorageFactory(IMongoClient client, bool useTransactions, string databaseName, Func<Type, string> collectionNamingConvention)
        {
            this.client = client;
            this.useTransactions = useTransactions;
            this.databaseName = databaseName;
            this.collectionNamingConvention = collectionNamingConvention;
        }

        public async Task<CompletableSynchronizedStorageSession> OpenSession(ContextBag contextBag)
        {
            var mongoSession = await client.StartSessionAsync().ConfigureAwait(false);

            if (useTransactions)
            {
                mongoSession.StartTransaction();
            }

            return new StorageSession(mongoSession, databaseName, contextBag, collectionNamingConvention);
        }

        readonly IMongoClient client;
        readonly bool useTransactions;
        readonly string databaseName;
        readonly Func<Type, string> collectionNamingConvention;
    }
}
