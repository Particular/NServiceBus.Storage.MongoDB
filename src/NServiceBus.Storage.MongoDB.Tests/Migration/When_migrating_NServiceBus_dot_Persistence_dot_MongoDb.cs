namespace NServiceBus.Storage.MongoDB.Tests
{
    using System;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using Persistence.ComponentTests;

    class When_migrating_NServiceBus_dot_Persistence_dot_MongoDb : SagaMigrationPersisterTests
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

            configuration = new PersistenceTestsConfiguration(nameof(legacySagaData.Version));

            await PrepareSagaCollection(legacySagaData, nameof(legacySagaData.SomeCorrelationPropertyId));

            var retrievedSagaData = await GetById<NServiceBusPersistenceMongoDBLegacySaga, NServiceBusPersistenceMongoDBLegacySagaData>(legacySagaData.Id);

            Assert.IsNotNull(retrievedSagaData, "Saga was not retrieved");
            Assert.AreEqual(legacySagaData.OriginalMessageId, retrievedSagaData.OriginalMessageId, "OriginalMessageId does not match");
            Assert.AreEqual(legacySagaData.Originator, retrievedSagaData.Originator, "Originator does not match");
            Assert.AreEqual(legacySagaData.SomeCorrelationPropertyId, retrievedSagaData.SomeCorrelationPropertyId, "SomeCorrelationPropertyId does not match");
            Assert.AreEqual(legacySagaData.SomeUpdatableSagaData, retrievedSagaData.SomeUpdatableSagaData, "SomeUpdatableSagaData does not match");
        }

        class NServiceBusPersistenceMongoDBLegacySaga : Saga<NServiceBusPersistenceMongoDBLegacySagaData>, IAmStartedByMessages<MigrationStartMessage>
        {
            public Task Handle(MigrationStartMessage message, IMessageHandlerContext context)
            {
                throw new NotImplementedException();
            }

            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<NServiceBusPersistenceMongoDBLegacySagaData> mapper)
            {
                mapper.ConfigureMapping<MigrationStartMessage>(msg => msg.Id).ToSaga(saga => saga.Id);
            }
        }

        class NServiceBusPersistenceMongoDBLegacySagaData : IContainSagaData
        {
            public Guid SomeCorrelationPropertyId { get; set; }

            public int SomeUpdatableSagaData { get; set; }
            public Guid Id { get; set; }

            public string OriginalMessageId { get; set; }

            public string Originator { get; set; }
        }
    }
}

namespace NServiceBus.Persistence.MongoDB
{
    using System;

    class NServiceBusPersistenceMongoDBLegacySagaData : IContainSagaData
    {
        public int Version { get; set; }

        public Guid SomeCorrelationPropertyId { get; set; }

        public int SomeUpdatableSagaData { get; set; }
        public Guid Id { get; set; }

        public string OriginalMessageId { get; set; }

        public string Originator { get; set; }
    }
}