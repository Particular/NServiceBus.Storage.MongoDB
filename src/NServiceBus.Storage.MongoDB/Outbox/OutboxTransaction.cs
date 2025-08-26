namespace NServiceBus.Storage.MongoDB;

using System;
using System.Threading;
using System.Threading.Tasks;
using Extensibility;
using global::MongoDB.Driver;
using Outbox;

class OutboxTransaction : IOutboxTransaction
{
    public OutboxTransaction(IClientSessionHandle mongoSession, string databaseName, MongoDatabaseSettings databaseSettings, ContextBag context,
        Func<Type, string> collectionNamingConvention, TimeSpan transactionTimeout)
    {
        StorageSession = new StorageSession(mongoSession, databaseName, databaseSettings, context, collectionNamingConvention, true,
            transactionTimeout);
        StorageSession.StartTransaction();
    }

    public StorageSession StorageSession { get; }

    public Task Commit(CancellationToken cancellationToken = default) => StorageSession.CommitTransaction(cancellationToken);

    public void Dispose() => StorageSession.Dispose();
}