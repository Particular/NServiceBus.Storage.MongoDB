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
                return CommitTransaction();
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

        public Task InsertOneAsync<T>(T document) => database.GetCollection<T>(collectionNamingConvention(typeof(T))).InsertOneAsync(MongoSession, document);

        public Task InsertOneAsync(Type type, BsonDocument document) => database.GetCollection<BsonDocument>(collectionNamingConvention(type)).InsertOneAsync(MongoSession, document);

        public Task<ReplaceOneResult> ReplaceOneAsync(Type type, FilterDefinition<BsonDocument> filter, BsonDocument document) => database.GetCollection<BsonDocument>(collectionNamingConvention(type)).ReplaceOneAsync(MongoSession, filter, document);

        public Task<DeleteResult> DeleteOneAsync(Type type, FilterDefinition<BsonDocument> filter) => database.GetCollection<BsonDocument>(collectionNamingConvention(type)).DeleteOneAsync(MongoSession, filter);

        public async Task<BsonDocument> Find<T>(FilterDefinition<BsonDocument> filter)
        {
            var collectionName = collectionNamingConvention(typeof(T));
            var sagaCollection = database.GetCollection<BsonDocument>(collectionName);
            var update = Builders<BsonDocument>.Update.Set("_lockToken", ObjectId.GenerateNewId());

            using (var cancellationTokenSource = new CancellationTokenSource(transactionTimeout))
            {
                var token = cancellationTokenSource.Token;
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        var result = await sagaCollection.FindOneAndUpdateAsync(MongoSession, filter, update, FindOneAndUpdateOptions, token)
                            .ConfigureAwait(false);
                        return result;
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (MongoCommandException e) when (WriteConflictUnderTransaction(e))
                    {
                        await AbortTransaction().ConfigureAwait(false);

                        try
                        {
                            await Task.Delay(TimeSpan.FromMilliseconds(random.Next(5, 20)), token).ConfigureAwait(false);
                        }
                        catch (OperationCanceledException)
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

        public async Task AbortTransaction()
        {
            if (useTransaction)
            {
                await MongoSession.AbortTransactionAsync().ConfigureAwait(false);
            }
        }

        public void StoreVersion<T>(int version) => contextBag.Set(typeof(T).FullName, version);

        public int RetrieveVersion(Type type) => contextBag.Get<int>(type.FullName);

        public Task CommitTransaction()
        {
            if (MongoSession.IsInTransaction)
            {
                return MongoSession.CommitTransactionAsync();
            }

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            if (MongoSession.IsInTransaction)
            {
                try
                {
                    MongoSession.AbortTransaction();
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