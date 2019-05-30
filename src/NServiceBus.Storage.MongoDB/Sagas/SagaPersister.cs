using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using NServiceBus.Extensibility;
using NServiceBus.Persistence;
using NServiceBus.Sagas;

namespace NServiceBus.Storage.MongoDB
{
    class SagaPersister : ISagaPersister
    {
        public SagaPersister(string versionFieldName)
        {
            this.versionFieldName = versionFieldName;
        }

        public async Task Save(IContainSagaData sagaData, SagaCorrelationProperty correlationProperty, SynchronizedStorageSession session, ContextBag context)
        {
            var storageSession = (StorageSession)session;
            var sagaDataType = sagaData.GetType();
            var collection = storageSession.GetCollection(sagaDataType);

            if (correlationProperty != null && !createdIndexCache.ContainsKey(sagaDataType.Name))
            {
                var uniqueFieldName = GetFieldName(BsonClassMap.LookupClassMap(sagaDataType), correlationProperty.Name);

                var indexModel = new CreateIndexModel<BsonDocument>(new BsonDocumentIndexKeysDefinition<BsonDocument>(new BsonDocument(uniqueFieldName, 1)), new CreateIndexOptions() { Unique = true });

                await collection.Indexes.CreateOneAsync(indexModel).ConfigureAwait(false);

                createdIndexCache.GetOrAdd(sagaDataType.Name, true);
            }

            var document = sagaData.ToBsonDocument();
            document.Add(versionFieldName, 0);

            await collection.InsertOneAsync(document).ConfigureAwait(false);
        }

        public async Task Update(IContainSagaData sagaData, SynchronizedStorageSession session, ContextBag context)
        {
            var storageSession = (StorageSession)session;
            var sagaDataType = sagaData.GetType();
            var collection = storageSession.GetCollection(sagaDataType);

            var version = storageSession.RetrieveVersion(sagaDataType);

            var fbuilder = Builders<BsonDocument>.Filter;
            var filter = fbuilder.Eq(idField, sagaData.Id) & fbuilder.Eq(versionFieldName, version);

            var bsonDoc = sagaData.ToBsonDocument();
            var ubuilder = Builders<BsonDocument>.Update;
            var update = ubuilder.Inc(versionFieldName, 1);

            foreach (var field in bsonDoc)
            {
                if (field.Name != versionFieldName && field.Name != idField)
                {
                    update = update.Set(field.Name, field.Value);
                }
            }

            var modifyResult = await collection.FindOneAndUpdateAsync(
                filter,
                update,
                new FindOneAndUpdateOptions<BsonDocument> { IsUpsert = false, ReturnDocument = ReturnDocument.After }).ConfigureAwait(false);

            if (modifyResult == null)
            {
                throw new Exception("Concurrency"); //TODO real exception message
            }
        }

        public async Task<TSagaData> Get<TSagaData>(Guid sagaId, SynchronizedStorageSession session, ContextBag context) where TSagaData : class, IContainSagaData
        {
            var storageSession = (StorageSession)session;
            var sagaDataType = typeof(TSagaData);
            var collection = storageSession.GetCollection(sagaDataType);

            var doc = await collection.Find(new BsonDocument(idField, sagaId)).FirstOrDefaultAsync().ConfigureAwait(false);

            if (doc != null)
            {
                var version = doc.GetValue(versionFieldName);
                doc.Remove(versionFieldName);
                storageSession.StoreVersion(sagaDataType, version);

                if (!BsonClassMap.IsClassMapRegistered(sagaDataType))
                {
                    BsonClassMap.RegisterClassMap<TSagaData>(cm =>
                    {
                        cm.AutoMap();
                        cm.SetIgnoreExtraElements(true);
                    });
                }

                return BsonSerializer.Deserialize<TSagaData>(doc);
            }

            return default;
        }

        public async Task<TSagaData> Get<TSagaData>(string propertyName, object propertyValue, SynchronizedStorageSession session, ContextBag context) where TSagaData : class, IContainSagaData
        {
            var storageSession = (StorageSession)session;
            var sagaDataType = typeof(TSagaData);
            var collection = storageSession.GetCollection(sagaDataType);

            var classmap = BsonClassMap.LookupClassMap(sagaDataType);
            var propertyFieldName = GetFieldName(classmap, propertyName);

            var doc = await collection.Find(new BsonDocument(propertyFieldName, BsonValue.Create(propertyValue))).Limit(1).FirstOrDefaultAsync().ConfigureAwait(false);

            if (doc != null)
            {
                var version = doc.GetValue(versionFieldName);
                doc.Remove(versionFieldName);
                storageSession.StoreVersion(sagaDataType, version);

                if (!BsonClassMap.IsClassMapRegistered(sagaDataType))
                {
                    BsonClassMap.RegisterClassMap<TSagaData>(cm =>
                    {
                        cm.AutoMap();
                        cm.SetIgnoreExtraElements(true);
                    });
                }

                return BsonSerializer.Deserialize<TSagaData>(doc);
            }

            return default;
        }

        public Task Complete(IContainSagaData sagaData, SynchronizedStorageSession session, ContextBag context)
        {
            var storageSession = (StorageSession)session;
            var sagaDataType = sagaData.GetType();
            var collection = storageSession.GetCollection(sagaDataType);

            return collection.DeleteOneAsync(new BsonDocument(idField, sagaData.Id));
        }

        string GetFieldName(BsonClassMap classMap, string property)
        {
            foreach(var element in classMap.AllMemberMaps)
            {
                if (element.MemberName == property)
                {
                    return element.ElementName;
                }
            }

            throw new ArgumentException($"Property '{property}' not found in class map.", nameof(property));
        }

        const string idField = "_id";
        readonly string versionFieldName;
        readonly ConcurrentDictionary<string, bool> createdIndexCache = new ConcurrentDictionary<string, bool>();
    }
}