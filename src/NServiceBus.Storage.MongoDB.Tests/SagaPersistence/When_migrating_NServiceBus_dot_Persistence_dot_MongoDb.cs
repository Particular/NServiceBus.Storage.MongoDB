using MongoDB.Bson;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace NServiceBus.Storage.MongoDB.Tests.SagaPersistence
{
    class When_migrating_NServiceBus_dot_Persistence_dot_MongoDb : MongoFixture
    {
        [Test]
        public async Task Persister_works_with_existing_sagas()
        {
            var legacySagaData = new Persistence.MongoDB.NServiceBusPersistenceMongoDBLegacySagaData
            {
                Id = Guid.NewGuid(),
                OriginalMessageId = Guid.NewGuid().ToString(),
                Originator = "SomeOriginator",
                Version = 5,
                SomeCorrelationPropertyId = Guid.NewGuid(),
                SomeUpdatableSagaData = GetHashCode()
            };

            SetVersionFieldName(nameof(legacySagaData.Version));

            await PrepareSagaCollection(legacySagaData, nameof(legacySagaData.SomeCorrelationPropertyId));

            var retrievedSagaData = await LoadSaga<NServiceBusPersistenceMongoDBLegacySagaData>(legacySagaData.Id);

            Assert.IsNotNull(retrievedSagaData, "Saga was not retrieved");
            Assert.AreEqual(legacySagaData.OriginalMessageId, retrievedSagaData.OriginalMessageId, "OriginalMessageId does not match");
            Assert.AreEqual(legacySagaData.Originator, retrievedSagaData.Originator, "Originator does not match");
            Assert.AreEqual(legacySagaData.SomeCorrelationPropertyId, retrievedSagaData.SomeCorrelationPropertyId, "SomeCorrelationPropertyId does not match");
            Assert.AreEqual(legacySagaData.SomeUpdatableSagaData, retrievedSagaData.SomeUpdatableSagaData, "SomeUpdatableSagaData does not match");
        }

        class NServiceBusPersistenceMongoDBLegacySagaData : IContainSagaData
        {
            public Guid Id { get; set; }
            public string OriginalMessageId { get; set; }
            public string Originator { get; set; }
            public Guid SomeCorrelationPropertyId { get; set; }
            public int SomeUpdatableSagaData { get; set; }
        }
    }
}

namespace NServiceBus.Persistence.MongoDB
{
    class NServiceBusPersistenceMongoDBLegacySagaData : IContainSagaData
    {
        public Guid Id { get; set; }
        public string OriginalMessageId { get; set; }
        public string Originator { get; set; }
        public int Version { get; set; }
        public Guid SomeCorrelationPropertyId { get; set; }
        public int SomeUpdatableSagaData { get; set; }
    }
}