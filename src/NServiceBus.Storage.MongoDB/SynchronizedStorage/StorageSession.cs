using System;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using NServiceBus.Extensibility;
using NServiceBus.Persistence;

namespace NServiceBus.Storage.MongoDB
{
    class StorageSession : CompletableSynchronizedStorageSession
    {
        public StorageSession(IMongoDatabase database, ContextBag contextBag, Func<Type, string> collectionNamingScheme)
        {
            this.database = database;
            this.contextBag = contextBag;
            this.collectionNamingScheme = collectionNamingScheme;
        }

        public IMongoCollection<BsonDocument> GetCollection(Type type) => database.GetCollection<BsonDocument>(collectionNamingScheme(type)).WithReadPreference(ReadPreference.Primary).WithWriteConcern(WriteConcern.WMajority);

        public void StoreVersion(Type type, BsonValue version) => contextBag.Set(type.FullName, version);

        public BsonValue RetrieveVersion(Type type) => contextBag.Get<BsonValue>(type.FullName);

        public Task CompleteAsync()
        {
            return TaskEx.CompletedTask;
        }

        public void Dispose()
        {

        }

        readonly IMongoDatabase database;
        readonly ContextBag contextBag;
        readonly Func<Type, string> collectionNamingScheme;
    }
}
