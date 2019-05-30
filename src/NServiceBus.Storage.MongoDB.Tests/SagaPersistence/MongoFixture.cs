using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using NServiceBus.Extensibility;
using NServiceBus.Persistence;
using NServiceBus.Sagas;
using NUnit.Framework;

namespace NServiceBus.Storage.MongoDB.Tests.SagaPersistence
{
    [TestFixture]
    public class MongoFixture
    {
        CompletableSynchronizedStorageSession session;
        SagaPersister sagaPersister;

        readonly string databaseName = "Test_" + DateTime.Now.Ticks.ToString(CultureInfo.InvariantCulture);
        string versionFieldName = "_version";
        Func<Type, string> collectionNameConvention = t => t.Name.ToLower();

        [SetUp]
        public virtual void SetupContext()
        {
            var storage = new SynchronizedStorageFactory(client: ClientProvider.Client, useTransactions: true, databaseName: databaseName, collectionNamingScheme: collectionNameConvention);

            session = storage.OpenSession(new ContextBag()).GetAwaiter().GetResult();
            sagaPersister = new SagaPersister(versionFieldName);
        }

        [TearDown]
        public void TeardownContext() => ClientProvider.Client.DropDatabase(databaseName);

        protected void SetVersionFieldName(string versionFieldName)
        {
            this.versionFieldName = versionFieldName;
            sagaPersister = new SagaPersister(versionFieldName);
        }

        protected void SetCollectionNamingConvention(Func<Type, string> convention)
        {
            collectionNameConvention = convention;

            var storage = new SynchronizedStorageFactory(client: ClientProvider.Client, useTransactions: true, databaseName: databaseName, collectionNamingScheme: convention);

            session = storage.OpenSession(new ContextBag()).GetAwaiter().GetResult();
        }

        protected Task PrepareSagaCollection<TSagaData>(TSagaData data, string correlationPropertyName) where TSagaData : IContainSagaData
        {
            return PrepareSagaCollection(data, correlationPropertyName, d => d.ToBsonDocument());
        }

        protected async Task PrepareSagaCollection<TSagaData>(TSagaData data, string correlationPropertyName, Func<TSagaData, BsonDocument> convertSagaData) where TSagaData : IContainSagaData
        {
            var sagaDataType = typeof(TSagaData);

            var document = convertSagaData(data);

            var collection = ClientProvider.Client.GetDatabase(databaseName).GetCollection<BsonDocument>(collectionNameConvention(sagaDataType));

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

            await sagaPersister.Save(saga, correlationProperty, session, null);
        }

        protected async Task<T> LoadSaga<T>(Guid id) where T : class, IContainSagaData
        {
            return await sagaPersister.Get<T>(id, session, null);
        }

        protected async Task CompleteSaga<T>(Guid sagaId) where T : class, IContainSagaData
        {
            var saga = await LoadSaga<T>(sagaId).ConfigureAwait(false);
            Assert.NotNull(saga);
            await sagaPersister.Complete(saga, session, null).ConfigureAwait(false);
        }

        protected async Task UpdateSaga<T>(Guid sagaId, Action<T> update) where T : class, IContainSagaData
        {
            var saga = await LoadSaga<T>(sagaId).ConfigureAwait(false);
            Assert.NotNull(saga, "Could not update saga. Saga not found");
            update(saga);
            await sagaPersister.Update(saga, session, null).ConfigureAwait(false);
        }

        protected void ChangeSagaVersionManually<T>(Guid sagaId, int version) where T : class, IContainSagaData
        {
            var collection = ClientProvider.Client.GetDatabase(databaseName).GetCollection<BsonDocument>(collectionNameConvention(typeof(T)));

            collection.UpdateOne(new BsonDocument("_id", sagaId), new BsonDocumentUpdateDefinition<BsonDocument>(
                new BsonDocument("$set", new BsonDocument(versionFieldName, version))));
        }
    }
}