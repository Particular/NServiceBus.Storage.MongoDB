﻿namespace NServiceBus.Storage.MongoDB
{
    using System;
    using System.Threading.Tasks;
    using Extensibility;
    using global::MongoDB.Bson;
    using global::MongoDB.Driver;
    using Logging;
    using Persistence;

    class StorageSession : CompletableSynchronizedStorageSession, IMongoSessionProvider
    {
        public StorageSession(IClientSessionHandle mongoSession, string databaseName, ContextBag contextBag, Func<Type, string> collectionNamingConvention, bool ownsMongoSession)
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
        }

        public IClientSessionHandle MongoSession { get; }

        Task CompletableSynchronizedStorageSession.CompleteAsync()
        {
            if (ownsMongoSession)
            {
                return CompleteAsync();
            }

            return TaskEx.CompletedTask;
        }

        void IDisposable.Dispose()
        {
            if (ownsMongoSession)
            {
                Dispose();
            }
        }

        public Task InsertOneAsync<T>(T document) => database.GetCollection<T>(collectionNamingConvention(typeof(T))).InsertOneAsync(MongoSession, document);

        public Task InsertOneAsync(Type type, BsonDocument document) => database.GetCollection<BsonDocument>(collectionNamingConvention(type)).InsertOneAsync(MongoSession, document);

        public Task<ReplaceOneResult> ReplaceOneAsync(Type type, FilterDefinition<BsonDocument> filter, BsonDocument document) => database.GetCollection<BsonDocument>(collectionNamingConvention(type)).ReplaceOneAsync(MongoSession, filter, document);

        public Task<DeleteResult> DeleteOneAsync(Type type, FilterDefinition<BsonDocument> filter) => database.GetCollection<BsonDocument>(collectionNamingConvention(type)).DeleteOneAsync(MongoSession, filter);

        public async Task<BsonDocument> Find<T>(FilterDefinition<BsonDocument> filter)
        {
            var sagaCollection = database.GetCollection<BsonDocument>(collectionNamingConvention(typeof(T)));
            var update = Builders<BsonDocument>.Update.Set("_lock", ObjectId.GenerateNewId());

            while (true)
            {
                try
                {
                    var result = await sagaCollection.FindOneAndUpdateAsync(MongoSession, filter, update, new FindOneAndUpdateOptions<BsonDocument>()
                    {
                        ReturnDocument = ReturnDocument.After
                    }).ConfigureAwait(false);
                    return result;
                }
                catch (MongoCommandException e)
                {
                    if (e.HasErrorLabel("TransientTransactionError"))
                    {
                        await Task.Delay(20).ConfigureAwait(false);
                        try
                        {
                            MongoSession.AbortTransaction();
                            MongoSession.StartTransaction(new TransactionOptions(ReadConcern.Majority, ReadPreference.Primary, WriteConcern.WMajority));
                        }
                        catch (Exception exception)
                        {
                            Console.WriteLine(exception);
                            throw;
                        }
                        continue;
                    }

                    throw;
                }
            }
        }

        public void StoreVersion<T>(int version) => contextBag.Set(typeof(T).FullName, version);

        public int RetrieveVersion(Type type) => contextBag.Get<int>(type.FullName);

        public Task CompleteAsync()
        {
            if (MongoSession.IsInTransaction)
            {
                return MongoSession.CommitTransactionAsync();
            }

            return TaskEx.CompletedTask;
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

        static readonly ILog Log = LogManager.GetLogger<StorageSession>();
    }
}