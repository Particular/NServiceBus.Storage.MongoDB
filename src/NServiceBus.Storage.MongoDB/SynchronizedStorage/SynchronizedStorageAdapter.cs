using NServiceBus.Extensibility;
using NServiceBus.Outbox;
using NServiceBus.Persistence;
using NServiceBus.Transport;
using System.Threading.Tasks;

namespace NServiceBus.Storage.MongoDB
{
    class SynchronizedStorageAdapter : ISynchronizedStorageAdapter
    {
        static readonly Task<CompletableSynchronizedStorageSession> emptyResult = Task.FromResult((CompletableSynchronizedStorageSession)null);

        public Task<CompletableSynchronizedStorageSession> TryAdapt(OutboxTransaction transaction, ContextBag context) => emptyResult;

        public Task<CompletableSynchronizedStorageSession> TryAdapt(TransportTransaction transportTransaction, ContextBag context) => emptyResult;
    }
}
