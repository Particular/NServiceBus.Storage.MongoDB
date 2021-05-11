namespace NServiceBus.Storage.MongoDB
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Extensibility;
    using global::MongoDB.Bson;
    using global::MongoDB.Driver;
    using Logging;
    using Persistence;

    class StorageSession : CompletableSynchronizedStorageSession, IMongoSessionProvider
    {
        public StorageSession(IClientSessionHandle mongoSession, string databaseName, ContextBag contextBag, Func<Type, string> collectionNamingConvention, bool ownsMongoSession, bool useTransaction, TimeSpan transactionTimeout)
        {
            MongoSession = mongoSession;

            database = mongoSession.Client.GetDatabase(databaseName, new MongoDatabaseSettings
            {
                ReadConcern = ReadConcern.Majority,
                ReadPreference = ReadPreference.Primary,
                WriteConcern = WriteConcern.WMajority
            });

            this.contextBag = contextBag;
            this.collectionNamingConvention = collectionNamingConvention;
            this.ownsMongoSession = ownsMongoSession;
            this.useTransaction = useTransaction;
            this.transactionTimeout = transactionTimeout;
        }

        Task CompletableSynchronizedStorageSession.CompleteAsync(CancellationToken cancellationToken)
        {
            if (ownsMongoSession)
            {
                return CommitTransaction(cancellationToken);
            }

            return Task.CompletedTask;
        }

        void IDisposable.Dispose()
        {
            if (ownsMongoSession)
            {
                Dispose();
            }
        }

        public IClientSessionHandle MongoSession { get; }

        public Task InsertOneAsync<T>(T document, CancellationToken cancellationToken = default) => database.GetCollection<T>(collectionNamingConvention(typeof(T))).InsertOneAsync(MongoSession, document, cancellationToken: cancellationToken);

        public Task InsertOneAsync(Type type, BsonDocument document, CancellationToken cancellationToken = default) => database.GetCollection<BsonDocument>(collectionNamingConvention(type)).InsertOneAsync(MongoSession, document, cancellationToken: cancellationToken);

        public Task<ReplaceOneResult> ReplaceOneAsync(Type type, FilterDefinition<BsonDocument> filter, BsonDocument document, CancellationToken cancellationToken = default) => database.GetCollection<BsonDocument>(collectionNamingConvention(type)).ReplaceOneAsync(MongoSession, filter, document, cancellationToken: cancellationToken);

        public Task<DeleteResult> DeleteOneAsync(Type type, FilterDefinition<BsonDocument> filter, CancellationToken cancellationToken = default) => database.GetCollection<BsonDocument>(collectionNamingConvention(type)).DeleteOneAsync(MongoSession, filter, cancellationToken: cancellationToken);

        public async Task<BsonDocument> Find<T>(FilterDefinition<BsonDocument> filter, CancellationToken cancellationToken = default)
        {
            var collectionName = collectionNamingConvention(typeof(T));
            var sagaCollection = database.GetCollection<BsonDocument>(collectionName);
            var update = Builders<BsonDocument>.Update.Set("_lockToken", ObjectId.GenerateNewId());

            using (var cancellationTokenSource = new CancellationTokenSource(transactionTimeout))
            {
                var timedToken = cancellationTokenSource.Token;
                var combinedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(timedToken, cancellationToken);
                var combinedToken = combinedTokenSource.Token;
                while (!timedToken.IsCancellationRequested)
                {
                    try
                    {
                        var result = await sagaCollection.FindOneAndUpdateAsync(MongoSession, filter, update, FindOneAndUpdateOptions, combinedToken)
                            .ConfigureAwait(false);
                        return result;
                    }
                    catch (OperationCanceledException) when (timedToken.IsCancellationRequested)
                    {
                        break;
                    }
                    catch (MongoCommandException e) when (WriteConflictUnderTransaction(e))
                    {
                        await AbortTransaction(combinedToken).ConfigureAwait(false);

                        try
                        {
                            await Task.Delay(TimeSpan.FromMilliseconds(random.Next(5, 20)), combinedToken).ConfigureAwait(false);
                        }
                        catch (OperationCanceledException) when (timedToken.IsCancellationRequested)
                        {
                            break;
                        }

                        StartTransaction();
                    }
                }

                throw new TimeoutException($"Unable to acquire exclusive write lock for saga on collection '{collectionName}'.");
            }
        }

        bool WriteConflictUnderTransaction(MongoCommandException e)
        {
            return useTransaction && e.HasErrorLabel("TransientTransactionError") && e.CodeName == "WriteConflict";
        }

        public void StartTransaction()
        {
            if (useTransaction)
            {
                MongoSession.StartTransaction(transactionOptions);
            }
        }

        public async Task AbortTransaction(CancellationToken cancellationToken = default)
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
        }

        readonly IMongoDatabase database;
        readonly ContextBag contextBag;
        readonly Func<Type, string> collectionNamingConvention;
        readonly bool ownsMongoSession;
        readonly bool useTransaction;

        readonly TimeSpan transactionTimeout;

        static Random random = new Random();
        static TransactionOptions transactionOptions = new TransactionOptions(ReadConcern.Majority, ReadPreference.Primary, WriteConcern.WMajority);

        static readonly ILog Log = LogManager.GetLogger<StorageSession>();

        static FindOneAndUpdateOptions<BsonDocument> FindOneAndUpdateOptions = new FindOneAndUpdateOptions<BsonDocument>
        {
            ReturnDocument = ReturnDocument.After
        };
    }
}