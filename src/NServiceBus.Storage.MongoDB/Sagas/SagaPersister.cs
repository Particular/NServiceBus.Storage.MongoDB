using MongoDB.Bson.Serialization;
using MongoDB.Bson;
using MongoDB.Driver;

namespace NServiceBus.Storage.MongoDB
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using NServiceBus.Extensibility;
    using NServiceBus.Persistence;
    using NServiceBus.Sagas;

    class SagaPersister : ISagaPersister
    {        
        public async Task Save(IContainSagaData sagaData, SagaCorrelationProperty correlationProperty, SynchronizedStorageSession session, ContextBag context)
        {
            var storageSession = (StorageSession)session;

            var collection = storageSession.GetCollection(sagaData.GetType());

            DocumentVersionAttribute.SetPropertyValue(sagaData, 0);
            await EnsureUniqueIndex(sagaData.GetType(), correlationProperty?.Name, collection).ConfigureAwait(false);

            await collection.InsertOneAsync(sagaData.ToBsonDocument()).ConfigureAwait(false);
        }

        private Task EnsureUniqueIndex(Type sagaDataType, string propertyName, IMongoCollection<BsonDocument> collection)
        {
            if (propertyName == null)
            {
                return Task.FromResult(0);
            }

            var classmap = BsonClassMap.LookupClassMap(sagaDataType);
            var uniqueFieldName = GetFieldName(classmap, propertyName);

            return collection.Indexes.CreateOneAsync(
                new BsonDocumentIndexKeysDefinition<BsonDocument>(new BsonDocument(propertyName, 1)), new CreateIndexOptions() { Unique = true });
        }

        public async Task Update(IContainSagaData sagaData, SynchronizedStorageSession session, ContextBag context)
        {
            var storageSession = (StorageSession)session;

            var versionProperty = DocumentVersionAttribute.GetProperty(sagaData);

            var classmap = BsonClassMap.LookupClassMap(sagaData.GetType());
            var versionFieldName = GetFieldName(classmap, versionProperty.Key);
            var version = versionProperty.Value;
            
            var collection = storageSession.GetCollection(sagaData.GetType());

            var fbuilder = Builders<BsonDocument>.Filter;
            var filter = fbuilder.Eq("_id", sagaData.Id) & fbuilder.Eq(versionFieldName, version);

            var bsonDoc = sagaData.ToBsonDocument();
            var ubuilder = Builders<BsonDocument>.Update;
            var update = ubuilder.Inc(versionFieldName, 1);

            foreach (var field in bsonDoc.Where(field => field.Name != versionFieldName && field.Name != "_id"))
            {
                update = update.Set(field.Name, field.Value);
            }

            var modifyResult = await collection.FindOneAndUpdateAsync(
                filter,
                update,
                new FindOneAndUpdateOptions<BsonDocument> { IsUpsert = false, ReturnDocument = ReturnDocument.After }).ConfigureAwait(false);

            if (modifyResult == null)
            {
                throw new MongoDBSagaConcurrentUpdateException(version);
            }
        }

        public Task<TSagaData> Get<TSagaData>(Guid sagaId, SynchronizedStorageSession session, ContextBag context) where TSagaData : class, IContainSagaData
        {
            return _repo.FindById<TSagaData>(sagaId);
        }

        public Task<TSagaData> Get<TSagaData>(string propertyName, object propertyValue, SynchronizedStorageSession session, ContextBag context) where TSagaData : class, IContainSagaData
        {
            var classmap = BsonClassMap.LookupClassMap(typeof(TSagaData));
            var propertyFieldName = GetFieldName(classmap, propertyName);

            return _repo.FindByFieldName<TSagaData>(propertyFieldName, propertyValue);
        }

        public Task Complete(IContainSagaData sagaData, SynchronizedStorageSession session, ContextBag context)
        {
            return _repo.Remove(sagaData);
        }

        private string GetFieldName(BsonClassMap classMap, string property)
        {
            var element = classMap.AllMemberMaps.First(m => m.MemberName == property);
            return element.ElementName;
        }
    }
}