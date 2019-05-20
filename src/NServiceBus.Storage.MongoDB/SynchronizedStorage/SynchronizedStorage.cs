using MongoDB.Driver;
using NServiceBus.Extensibility;
using NServiceBus.Persistence;
using System;
using System.Threading.Tasks;

namespace NServiceBus.Storage.MongoDB
{
    class SynchronizedStorage : ISynchronizedStorage
    {
        public SynchronizedStorage(IMongoDatabase database, Func<Type, string> collectionNamingScheme)
        {
            this.database = database;
            this.collectionNamingScheme = collectionNamingScheme;
        }

        public Task<CompletableSynchronizedStorageSession> OpenSession(ContextBag contextBag)
        {
            return Task.FromResult((CompletableSynchronizedStorageSession)new StorageSession(database, contextBag, collectionNamingScheme));
        }

        readonly IMongoDatabase database;
        readonly Func<Type, string> collectionNamingScheme;
    }
}
