namespace NServiceBus.Storage.MongoDB;

using System;
using System.Threading;
using System.Threading.Tasks;
using Extensibility;
using global::MongoDB.Driver;
using NServiceBus.Outbox;

sealed class MongoOutboxTransactionFactory(
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

        return new MongoOutboxTransaction(mongoSession, databaseName, databaseSettings, context, collectionNamingConvention,
            transactionTimeout);
    }
}