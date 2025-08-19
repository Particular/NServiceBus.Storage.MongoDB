namespace NServiceBus.Storage.MongoDB;

using System;
using System.Threading;
using System.Threading.Tasks;
using Extensibility;
using global::MongoDB.Bson;
using global::MongoDB.Driver;
using Logging;

class StorageSession(
    IClientSessionHandle mongoSession,
    string databaseName,
    MongoDatabaseSettings databaseSettings,
    ContextBag contextBag,
    Func<Type, string> collectionNamingConvention,
    bool useTransaction,
    TimeSpan transactionTimeout)
{
    public IClientSessionHandle MongoSession { get; } = mongoSession;

    public Task InsertOneAsync<T>(T document, CancellationToken cancellationToken = default) => database
        .GetCollection<T>(collectionNamingConvention(typeof(T)))
        .InsertOneAsync(MongoSession, document, cancellationToken: cancellationToken);

    public Task InsertOneAsync(Type type, BsonDocument document, CancellationToken cancellationToken = default) =>
        database.GetCollection<BsonDocument>(collectionNamingConvention(type))
            .InsertOneAsync(MongoSession, document, cancellationToken: cancellationToken);

    public Task<ReplaceOneResult> ReplaceOneAsync(Type type, FilterDefinition<BsonDocument> filter,
        BsonDocument document, CancellationToken cancellationToken = default) => database
        .GetCollection<BsonDocument>(collectionNamingConvention(type)).ReplaceOneAsync(MongoSession, filter, document,
            cancellationToken: cancellationToken);

    public Task<DeleteResult> DeleteOneAsync(Type type, FilterDefinition<BsonDocument> filter,
        CancellationToken cancellationToken = default) => database
        .GetCollection<BsonDocument>(collectionNamingConvention(type))
        .DeleteOneAsync(MongoSession, filter, cancellationToken: cancellationToken);

    public async Task<BsonDocument> Find<T>(FilterDefinition<BsonDocument> filter,
        CancellationToken cancellationToken = default)
    {
        var collectionName = collectionNamingConvention(typeof(T));
        var sagaCollection = database.GetCollection<BsonDocument>(collectionName);
        var update = Builders<BsonDocument>.Update.Set("_lockToken", ObjectId.GenerateNewId());

        using var timedTokenSource = new CancellationTokenSource(transactionTimeout);
        using var combinedTokenSource =
            CancellationTokenSource.CreateLinkedTokenSource(timedTokenSource.Token, cancellationToken);
        while (!timedTokenSource.IsCancellationRequested)
        {
            try
            {
                try
                {
                    return await sagaCollection.FindOneAndUpdateAsync(MongoSession, filter, update,
                        FindOneAndUpdateOptions, combinedTokenSource.Token).ConfigureAwait(false);
                }
                catch (MongoCommandException e) when (WriteConflictUnderTransaction(e))
                {
                    await AbortTransaction(combinedTokenSource.Token).ConfigureAwait(false);
                    await Task.Delay(TimeSpan.FromMilliseconds(Random.Shared.Next(5, 20)), combinedTokenSource.Token)
                        .ConfigureAwait(false);
                    StartTransaction();
                }
            }
#pragma warning disable PS0019 // When catching System.Exception, cancellation needs to be properly accounted for - justification:
            // Cancellation is properly accounted for. In this case, we only want to catch cancellation by one of the tokens used to create the combined token.
            catch (Exception ex) when (ex.IsCausedBy(timedTokenSource.Token))
#pragma warning restore PS0019 // When catching System.Exception, cancellation needs to be properly accounted for
            {
                // log the exception in case the stack trace will ever be useful for debugging
                Log.Debug("Operation canceled when time out exhausted for acquiring exclusive write lock.", ex);
                break;
            }
        }

        throw new TimeoutException(
            $"Unable to acquire exclusive write lock for saga on collection '{collectionName}'.");
    }

    bool WriteConflictUnderTransaction(MongoCommandException e) => useTransaction && e.HasErrorLabel("TransientTransactionError") && e.CodeName == "WriteConflict";

    public void StartTransaction()
    {
        if (useTransaction)
        {
            MongoSession.StartTransaction(transactionOptions);
        }
    }

    async Task AbortTransaction(CancellationToken cancellationToken)
    {
        if (useTransaction)
        {
            await MongoSession.AbortTransactionAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    public void StoreVersion<T>(int version) => contextBag.Set(typeof(T).FullName, version);

    public int RetrieveVersion(Type type) => contextBag.Get<int>(type.FullName);

    public Task CommitTransaction(CancellationToken cancellationToken = default)
    {
        if (MongoSession.IsInTransaction)
        {
            return MongoSession.CommitTransactionAsync(cancellationToken);
        }

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        if (MongoSession.IsInTransaction)
        {
            try
            {
                // Once you track it down, calling AbortTransaction with CancellationToken.None is exactly what the MongoDB driver
                // does on dispose: https://github.com/mongodb/mongo-csharp-driver/blob/v2.12.0/src/MongoDB.Driver.Core/Core/Bindings/CoreSession.cs#L326-L351
                // so while it might be a *bit* on the defensive side, it does allow us to capture and warn on the exception without doing any worse
                // than the Mongo client.
                MongoSession.AbortTransaction(CancellationToken.None);
            }
            catch (Exception ex)
            {
                Log.Warn("Exception thrown while aborting transaction", ex);
            }
        }

        MongoSession.Dispose();
        disposed = true;
    }

    bool disposed;

    readonly IMongoDatabase database = mongoSession.Client.GetDatabase(databaseName, databaseSettings);

    static readonly TransactionOptions transactionOptions = new(ReadConcern.Majority, ReadPreference.Primary, WriteConcern.WMajority);

    static readonly ILog Log = LogManager.GetLogger<StorageSession>();

    static readonly FindOneAndUpdateOptions<BsonDocument> FindOneAndUpdateOptions = new() { ReturnDocument = ReturnDocument.After };
}