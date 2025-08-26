namespace NServiceBus.Storage.MongoDB;

using System;
using System.Threading;
using System.Threading.Tasks;
using Extensibility;
using global::MongoDB.Driver;
using Outbox;

sealed class OutboxTransactionFactory(
    IMongoClient client,
    string databaseName,
    MongoDatabaseSettings databaseSettings,
    Func<Type, string> collectionNamingConvention,
    TimeSpan transactionTimeout)
{
    public async Task<IOutboxTransaction> BeginTransaction(ContextBag context,
        CancellationToken cancellationToken = default)
    {
        var mongoSession = await client.StartSessionAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

        return new OutboxTransaction(mongoSession, databaseName, databaseSettings, context, collectionNamingConvention,
            transactionTimeout);
    }
}