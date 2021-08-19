namespace NServiceBus.Storage.MongoDB
{
    using System.Threading;
    using System.Threading.Tasks;
    using Extensibility;
    using Outbox;
    using Persistence;
    using Transport;

    class StorageSessionAdapter : ISynchronizedStorageAdapter
    {
        public Task<ICompletableSynchronizedStorageSession> TryAdapt(IOutboxTransaction transaction, ContextBag context, CancellationToken cancellationToken = default)
        {
            if (transaction is MongoOutboxTransaction mongoOutboxTransaction)
            {
                return Task.FromResult((ICompletableSynchronizedStorageSession)mongoOutboxTransaction.StorageSession);
            }

            return emptyResult;
        }

        public Task<ICompletableSynchronizedStorageSession> TryAdapt(TransportTransaction transportTransaction, ContextBag context, CancellationToken cancellationToken = default) => emptyResult;

        static readonly Task<ICompletableSynchronizedStorageSession> emptyResult = Task.FromResult((ICompletableSynchronizedStorageSession)null);
    }
}