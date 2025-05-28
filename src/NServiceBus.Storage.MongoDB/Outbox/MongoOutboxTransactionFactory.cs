namespace NServiceBus.Storage.MongoDB;

using System;
using System.Threading;
using System.Threading.Tasks;
using Extensibility;
using global::MongoDB.Driver;
using Outbox;

class MongoOutboxTransactionFactory
{
    public MongoOutboxTransactionFactory(IMongoClient client, string databaseName,
        Func<Type, string> collectionNamingConvention, TimeSpan transactionTimeout)
    {
        this.transactionTimeout = transactionTimeout;
        this.client = client;
        this.databaseName = databaseName;
        this.collectionNamingConvention = collectionNamingConvention;
    }

    public async Task<IOutboxTransaction> BeginTransaction(ContextBag context,
        CancellationToken cancellationToken = default)
    {
        var mongoSession = await client.StartSessionAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

        return new MongoOutboxTransaction(mongoSession, databaseName, context, collectionNamingConvention,
            transactionTimeout);
    }

    readonly IMongoClient client;
    readonly string databaseName;
    readonly Func<Type, string> collectionNamingConvention;
    readonly TimeSpan transactionTimeout;
}