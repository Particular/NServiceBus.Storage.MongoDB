namespace NServiceBus.Storage.MongoDB.Tests.SagaPersistence
{
    using System;
    using System.Globalization;
    using System.Threading.Tasks;
    using global::MongoDB.Bson;
    using global::MongoDB.Bson.Serialization.Conventions;
    using global::MongoDB.Driver;
    using NServiceBus.Extensibility;
    using NServiceBus.Persistence;
    using NServiceBus.Sagas;
    using NUnit.Framework;

    [TestFixture]
    public class MongoFixture
    {
        private IMongoDatabase _database;
        private CompletableSynchronizedStorageSession _session;
        private SagaPersister _sagaPersister;
        private MongoClient _client;
        private readonly string _databaseName = "Test_" + DateTime.Now.Ticks.ToString(CultureInfo.InvariantCulture);
        private string versionFieldName = "_version";

        [SetUp]
        public virtual void SetupContext()
        {

            var camelCasePack = new ConventionPack { new CamelCaseElementNameConvention() };
            ConventionRegistry.Register("CamelCase", camelCasePack, type => true);

            var connectionString = ConnectionStringProvider.GetConnectionString();

            _client = new MongoClient(connectionString);
            _database = _client.GetDatabase(_databaseName);
            _session = new StorageSession(_database, new ContextBag(), type => type.Name.ToLower());

            _sagaPersister = new SagaPersister(versionFieldName);
        }

        [TearDown]
        public void TeardownContext() => _client.DropDatabase(_databaseName);

        protected void SetVersionFieldName(string versionFieldName)
        {
            this.versionFieldName = versionFieldName;
            _sagaPersister = new SagaPersister(versionFieldName);
        }

        protected Task EnsureUniqueIndex(IMongoCollection<BsonDocument> collection, IContainSagaData saga, string correlationPropertyName)
        {
            return _sagaPersister.EnsureUniqueIndex(saga.GetType(), correlationPropertyName, collection);
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
            var collection = _database.GetCollection<BsonDocument>(typeof(T).Name.ToLower());

            collection.UpdateOne(new BsonDocument("_id", sagaId), new BsonDocumentUpdateDefinition<BsonDocument>(
                new BsonDocument("$set", new BsonDocument(versionFieldName, version))));
        }
    }
}