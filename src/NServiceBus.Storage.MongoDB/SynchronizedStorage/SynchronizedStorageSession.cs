namespace NServiceBus.Storage.MongoDB
{
    using System.Threading;
    using System.Threading.Tasks;
    using Extensibility;
    using global::MongoDB.Driver;
    using Outbox;
    using Persistence;
    using Transport;

    class SynchronizedStorageSession : ICompletableSynchronizedStorageSession, IMongoSessionProvider
    {
        public StorageSession Session { get; private set; }
        public IClientSessionHandle MongoSession => Session.MongoSession;

        public SynchronizedStorageSession(StorageSessionFactory sessionFactory) => this.sessionFactory = sessionFactory;

        public ValueTask<bool> TryOpen(IOutboxTransaction transaction, ContextBag context, CancellationToken cancellationToken = default)
        {
            if (transaction is MongoOutboxTransaction mongoOutboxTransaction)
            {
                Session = mongoOutboxTransaction.StorageSession;
                ownsMongoSession = false;
                return new ValueTask<bool>(true);
            }

            return new ValueTask<bool>(false);
        }

        public ValueTask<bool> TryOpen(TransportTransaction transportTransaction, ContextBag context, CancellationToken cancellationToken = default) => new ValueTask<bool>(false);

        public async Task Open(ContextBag contextBag, CancellationToken cancellationToken = default)
        {
            Session = await sessionFactory.OpenSession(contextBag, cancellationToken).ConfigureAwait(false);
            ownsMongoSession = true;
        }
        public void Dispose()
        {
            if (ownsMongoSession && Session != null)
            {
                Session.Dispose();
                Session = null;
            }
        }

        public Task CompleteAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            if (ownsMongoSession)
            {
                return Session.CommitTransaction(cancellationToken);
            }

            return Task.CompletedTask;
        }

        readonly StorageSessionFactory sessionFactory;
        bool ownsMongoSession;
    }
}