using System.Threading.Tasks;
using NServiceBus.Extensibility;
using NServiceBus.Outbox;
using NServiceBus.Persistence;
using NServiceBus.Transport;

namespace NServiceBus.Storage.MongoDB
{
    class SynchronizedStorageAdapter : ISynchronizedStorageAdapter
    {
        static readonly Task<CompletableSynchronizedStorageSession> emptyResult = Task.FromResult((CompletableSynchronizedStorageSession)null);

        public Task<CompletableSynchronizedStorageSession> TryAdapt(OutboxTransaction transaction, ContextBag context)
        {
            if (transaction is MongoOutboxTransaction mongoOutboxTransaction)
            {
                return Task.FromResult((CompletableSynchronizedStorageSession)mongoOutboxTransaction.StorageSession);
            }

            return emptyResult;
        }

        public Task<CompletableSynchronizedStorageSession> TryAdapt(TransportTransaction transportTransaction, ContextBag context) => emptyResult;
    }
}
