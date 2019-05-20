using MongoDB.Driver;
using NServiceBus.Extensibility;
using NServiceBus.Persistence;
using System.Threading.Tasks;

namespace NServiceBus.Storage.MongoDB
{
    class SynchronizedStorage : ISynchronizedStorage
    {
        public SynchronizedStorage(IMongoDatabase database)
        {
            this.database = database;
        }

        public Task<CompletableSynchronizedStorageSession> OpenSession(ContextBag contextBag)
        {
            return Task.FromResult((CompletableSynchronizedStorageSession)new StorageSession(database, contextBag));
        }

        readonly IMongoDatabase database;
    }
}
