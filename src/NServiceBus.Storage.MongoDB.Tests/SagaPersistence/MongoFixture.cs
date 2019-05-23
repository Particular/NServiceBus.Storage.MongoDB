namespace NServiceBus.Storage.MongoDB.Tests.SagaPersistence
{
    using System;
    using System.Globalization;
    using System.Linq;
    using System.Threading.Tasks;
    using global::MongoDB.Bson;
    using global::MongoDB.Bson.Serialization;
    using global::MongoDB.Driver;
    using NServiceBus.Extensibility;
    using NServiceBus.Persistence;
    using NServiceBus.Sagas;
    using NUnit.Framework;

    [TestFixture]
    public class MongoFixture
    {
        private CompletableSynchronizedStorageSession _session;
        private SagaPersister _sagaPersister;

        private readonly string _databaseName = "Test_" + DateTime.Now.Ticks.ToString(CultureInfo.InvariantCulture);
        private string versionFieldName = "_version";
        Func<Type, string> collectionNameConvention = t => t.Name.ToLower();

        [SetUp]
        public virtual void SetupContext()
        {
            var storage = new SynchronizedStorage(ClientProvider.Client, _databaseName, collectionNameConvention);

            _session = storage.OpenSession(new ContextBag()).GetAwaiter().GetResult();
            _sagaPersister = new SagaPersister(versionFieldName);
        }

        [TearDown]
        public void TeardownContext() => ClientProvider.Client.DropDatabase(_databaseName);

        protected void SetVersionFieldName(string versionFieldName)
        {
            this.versionFieldName = versionFieldName;
            _sagaPersister = new SagaPersister(versionFieldName);
        }

        protected void SetCollectionNamingConvention(Func<Type, string> convention)
        {
            collectionNameConvention = convention;

            var storage = new SynchronizedStorage(ClientProvider.Client, _databaseName, convention);

            _session = storage.OpenSession(new ContextBag()).GetAwaiter().GetResult();
        }

        protected Task PrepareSagaCollection<TSagaData>(TSagaData data, string correlationPropertyName) where TSagaData : IContainSagaData
        {
            return PrepareSagaCollection(data, correlationPropertyName, d => d.ToBsonDocument());     
        }

        protected async Task PrepareSagaCollection<TSagaData>(TSagaData data, string correlationPropertyName, Func<TSagaData, BsonDocument> convertSagaData) where TSagaData: IContainSagaData
        {
            var sagaDataType = typeof(TSagaData);

            var document = convertSagaData(data);

            var collection = ClientProvider.Client.GetDatabase(_databaseName).GetCollection<BsonDocument>(collectionNameConvention(sagaDataType));

            var uniqueFieldName = BsonClassMap.LookupClassMap(sagaDataType).AllMemberMaps.First(m => m.MemberName == correlationPropertyName).ElementName;

            var indexModel = new CreateIndexModel<BsonDocument>(new BsonDocumentIndexKeysDefinition<BsonDocument>(new BsonDocument(uniqueFieldName, 1)), new CreateIndexOptions() { Unique = true });

            await collection.Indexes.CreateOneAsync(indexModel);

            await collection.InsertOneAsync(document);
        }

        protected async Task SaveSaga<T>(T saga) where T : class, IContainSagaData
        {
            SagaCorrelationProperty correlationProperty = null;

            if (saga.GetType() == typeof(SagaWithUniqueProperty))
            {
                correlationProperty = new SagaCorrelationProperty("UniqueString", String.Empty);
            }

            await _sagaPersister.Save(saga, correlationProperty, _session, null);
        }

        protected async Task<T> LoadSaga<T>(Guid id) where T : class, IContainSagaData
        {
            return await _sagaPersister.Get<T>(id, _session, null);
        }

        protected async Task CompleteSaga<T>(Guid sagaId) where T : class, IContainSagaData
        {
            var saga = await LoadSaga<T>(sagaId).ConfigureAwait(false);
            Assert.NotNull(saga);
            await _sagaPersister.Complete(saga, _session, null).ConfigureAwait(false);
        }

        protected async Task UpdateSaga<T>(Guid sagaId, Action<T> update) where T : class, IContainSagaData
        {
            var saga = await LoadSaga<T>(sagaId).ConfigureAwait(false);
            Assert.NotNull(saga, "Could not update saga. Saga not found");
            update(saga);
            await _sagaPersister.Update(saga, _session, null).ConfigureAwait(false);
        }

        protected void ChangeSagaVersionManually<T>(Guid sagaId, int version) where T : class, IContainSagaData
        {
            var collection = ClientProvider.Client.GetDatabase(_databaseName).GetCollection<BsonDocument>(collectionNameConvention(typeof(T)));

            collection.UpdateOne(new BsonDocument("_id", sagaId), new BsonDocumentUpdateDefinition<BsonDocument>(
                new BsonDocument("$set", new BsonDocument(versionFieldName, version))));
        }
    }
}