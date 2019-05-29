using System;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using NServiceBus.Extensibility;
using NServiceBus.Logging;
using NServiceBus.Persistence;

namespace NServiceBus.Storage.MongoDB
{
    class StorageSession : CompletableSynchronizedStorageSession
    {
        public StorageSession(IClientSessionHandle mongoSession, string databaseName, ContextBag contextBag, Func<Type, string> collectionNamingScheme)
        {
            this.mongoSession = mongoSession;

            database = mongoSession.Client.GetDatabase(databaseName, new MongoDatabaseSettings
            {
                ReadPreference = ReadPreference.Primary,
                WriteConcern = WriteConcern.WMajority
            });

            this.contextBag = contextBag;
            this.collectionNamingScheme = collectionNamingScheme;
        }

        public IMongoCollection<BsonDocument> GetCollection(Type type) => database.GetCollection<BsonDocument>(collectionNamingScheme(type));

        public IMongoCollection<T> GetCollection<T>(string name, MongoCollectionSettings settings = null) => database.GetCollection<T>(name, settings);

        public void StoreVersion(Type type, BsonValue version) => contextBag.Set(type.FullName, version);

        public BsonValue RetrieveVersion(Type type) => contextBag.Get<BsonValue>(type.FullName);

        public Task CompleteAsync()
        {
            if (mongoSession.IsInTransaction)
            {
                return mongoSession.CommitTransactionAsync();
            }

            return TaskEx.CompletedTask;
        }

        public void Dispose()
        {
            if (mongoSession.IsInTransaction)
            {
                try
                {
                    mongoSession.AbortTransaction();
                }
                catch (Exception ex)
                {
                    Log.Warn("Exception thrown while aborting transaction", ex);
                }
            }

            mongoSession.Dispose();
        }

        static readonly ILog Log = LogManager.GetLogger<StorageSession>();

        readonly IClientSessionHandle mongoSession;
        readonly IMongoDatabase database;
        readonly ContextBag contextBag;
        readonly Func<Type, string> collectionNamingScheme;
    }
}
