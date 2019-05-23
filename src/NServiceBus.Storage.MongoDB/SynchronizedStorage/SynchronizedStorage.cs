using MongoDB.Driver;
using NServiceBus.Extensibility;
using NServiceBus.Persistence;
using System;
using System.Threading.Tasks;

namespace NServiceBus.Storage.MongoDB
{
    class SynchronizedStorage : ISynchronizedStorage
    {
        public SynchronizedStorage(IMongoClient client, string databaseName, Func<Type, string> collectionNamingScheme)
        {
            this.client = client;
            this.databaseName = databaseName;
            this.collectionNamingScheme = collectionNamingScheme;
        }

        public async Task<CompletableSynchronizedStorageSession> OpenSession(ContextBag contextBag)
        {
            var mongoSession = await client.StartSessionAsync().ConfigureAwait(false);

            return new StorageSession(mongoSession, databaseName, contextBag, collectionNamingScheme);
        }

        readonly IMongoClient client;
        readonly string databaseName;
        readonly Func<Type, string> collectionNamingScheme;
    }
}
