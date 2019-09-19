namespace NServiceBus.Storage.MongoDB
{
    using System.Threading.Tasks;
    using Extensibility;
    using Outbox;
    using Persistence;
    using Transport;

    class StorageSessionAdapter : ISynchronizedStorageAdapter
    {
        public Task<CompletableSynchronizedStorageSession> TryAdapt(OutboxTransaction transaction, ContextBag context)
        {
            if (transaction is MongoOutboxTransaction mongoOutboxTransaction)
            {
                return Task.FromResult((CompletableSynchronizedStorageSession)mongoOutboxTransaction.StorageSession);
            }

            return emptyResult;
        }

        public Task<CompletableSynchronizedStorageSession> TryAdapt(TransportTransaction transportTransaction, ContextBag context) => emptyResult;
        static readonly Task<CompletableSynchronizedStorageSession> emptyResult = Task.FromResult((CompletableSynchronizedStorageSession)null);
    }
}