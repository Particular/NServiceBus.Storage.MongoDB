using MongoBson = MongoDB.Bson;
using MongoDB.Bson;
using MongoDB.Driver;

namespace NServiceBus.Storage.MongoDB
{
    using System;
    using System.Threading.Tasks;

    abstract class RepositoryBase
    {
        protected IMongoDatabase Database { get; private set; }

        protected RepositoryBase(IMongoDatabase database)
        {
            Database = database;
        }

        protected string GetCollectionName(Type entityType)
        {
            return entityType.Name.ToLower();
        }

        protected IMongoCollection<BsonDocument> GetCollection<T>()
        {
            return GetCollection(typeof (T));
        }

        protected IMongoCollection<BsonDocument> GetCollection(Type type)
        {
            return Database.GetCollection<BsonDocument>(GetCollectionName(type)).WithReadPreference(ReadPreference.Primary).WithWriteConcern(WriteConcern.WMajority);
        }

        public Task EnsureUniqueIndex(Type entityType, string fieldName)
        {
            return GetCollection(entityType).Indexes.CreateOneAsync(
                new BsonDocumentIndexKeysDefinition<BsonDocument>(new BsonDocument(fieldName, 1)), new CreateIndexOptions() { Unique = true });
        }

        protected static T Deserialize<T>(BsonDocument doc)
        {
            if (doc == null)
            {
                return default(T);
            }

            return MongoBson.Serialization.BsonSerializer.Deserialize<T>(doc);
        }
    }
}