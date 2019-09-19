namespace NServiceBus.Storage.MongoDB
{
    using System;
    using System.Threading.Tasks;
    using Extensibility;
    using global::MongoDB.Bson;
    using global::MongoDB.Bson.Serialization;
    using global::MongoDB.Driver;
    using Persistence;
    using Sagas;

    class SagaPersister : ISagaPersister
    {
        public SagaPersister(string versionElementName)
        {
            this.versionElementName = versionElementName;
        }

        public async Task Save(IContainSagaData sagaData, SagaCorrelationProperty correlationProperty, SynchronizedStorageSession session, ContextBag context)
        {
            var storageSession = (StorageSession)session;
            var sagaDataType = sagaData.GetType();

            var document = sagaData.ToBsonDocument();
            document.Add(versionElementName, 0);

            await storageSession.InsertOneAsync(sagaDataType, document).ConfigureAwait(false);
        }

        public async Task Update(IContainSagaData sagaData, SynchronizedStorageSession session, ContextBag context)
        {
            var storageSession = (StorageSession)session;
            var sagaDataType = sagaData.GetType();

            var version = storageSession.RetrieveVersion(sagaDataType);
            var document = sagaData.ToBsonDocument().SetElement(new BsonElement(versionElementName, version + 1));

            var result = await storageSession.ReplaceOneAsync(sagaDataType, filterBuilder.Eq(idElementName, sagaData.Id) & filterBuilder.Eq(versionElementName, version), document).ConfigureAwait(false);

            if (result.ModifiedCount != 1)
            {
                throw new Exception($"The '{sagaDataType.Name}' saga with id '{sagaData.Id}' was updated by another process or no longer exists.");
            }
        }

        public Task<TSagaData> Get<TSagaData>(Guid sagaId, SynchronizedStorageSession session, ContextBag context) where TSagaData : class, IContainSagaData =>
            GetSagaData<TSagaData>(idElementName, sagaId, session);

        public Task<TSagaData> Get<TSagaData>(string propertyName, object propertyValue, SynchronizedStorageSession session, ContextBag context) where TSagaData : class, IContainSagaData =>
            GetSagaData<TSagaData>(typeof(TSagaData).GetElementName(propertyName), propertyValue, session);

        public async Task Complete(IContainSagaData sagaData, SynchronizedStorageSession session, ContextBag context)
        {
            var storageSession = (StorageSession)session;
            var sagaDataType = sagaData.GetType();

            var version = storageSession.RetrieveVersion(sagaDataType);

            var result = await storageSession.DeleteOneAsync(sagaDataType, filterBuilder.Eq(idElementName, sagaData.Id) & filterBuilder.Eq(versionElementName, version)).ConfigureAwait(false);

            if (result.DeletedCount != 1)
            {
                throw new Exception("Saga can't be completed because it was updated by another process.");
            }
        }

        async Task<TSagaData> GetSagaData<TSagaData>(string elementName, object elementValue, SynchronizedStorageSession session)
        {
            var storageSession = (StorageSession)session;

            var document = await storageSession.Find<TSagaData>(new BsonDocument(elementName, BsonValue.Create(elementValue))).SingleOrDefaultAsync().ConfigureAwait(false);

            if (document != null)
            {
                var version = document.GetValue(versionElementName);
                storageSession.StoreVersion<TSagaData>(version.AsInt32);

                return BsonSerializer.Deserialize<TSagaData>(document);
            }

            return default;
        }

        readonly string versionElementName;
        readonly FilterDefinitionBuilder<BsonDocument> filterBuilder = Builders<BsonDocument>.Filter;

        const string idElementName = "_id";
    }
}