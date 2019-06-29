using System;
using System.Threading.Tasks;
using MongoDB.Bson;
using NUnit.Framework;

namespace NServiceBus.Persistence.ComponentTests
{
    class When_migrating_NServiceBus_dot_MongoDB : SagaMigrationPersisterTests
    {
        readonly Func<Type, string> collectionNamingConvention = t => t.Name;

        [Test]
        public async Task Persister_works_with_existing_sagas()
        {
            var legacySagaData = new NServiceBus.MongoDB.NServiceBusMongoDBLegacySagaData
            {
                Id = Guid.NewGuid(),
                OriginalMessageId = Guid.NewGuid().ToString(),
                Originator = "SomeOriginator",
                DocumentVersion = 5,
                SomeCorrelationPropertyId = Guid.NewGuid(),
                SomeUpdatableSagaData = GetHashCode()
            };

            configuration = new PersistenceTestsConfiguration(nameof(legacySagaData.DocumentVersion), collectionNamingConvention);

            await PrepareSagaCollection(legacySagaData, nameof(legacySagaData.SomeCorrelationPropertyId), d =>
            {
                var document = d.ToBsonDocument();

                d.ETag = document.GetHashCode();

                return d.ToBsonDocument();
            });

            var retrievedSagaData = await GetById<NServiceBusMongoDBLegacySaga,NServiceBusMongoDBLegacySagaData>(legacySagaData.Id);

            Assert.IsNotNull(retrievedSagaData, "Saga was not retrieved");
            Assert.AreEqual(legacySagaData.OriginalMessageId, retrievedSagaData.OriginalMessageId, "OriginalMessageId does not match");
            Assert.AreEqual(legacySagaData.Originator, retrievedSagaData.Originator, "Originator does not match");
            Assert.AreEqual(legacySagaData.SomeCorrelationPropertyId, retrievedSagaData.SomeCorrelationPropertyId, "SomeCorrelationPropertyId does not match");
            Assert.AreEqual(legacySagaData.SomeUpdatableSagaData, retrievedSagaData.SomeUpdatableSagaData, "SomeUpdatableSagaData does not match");
        }

        class NServiceBusMongoDBLegacySaga : Saga<NServiceBusMongoDBLegacySagaData>, IAmStartedByMessages<MigrationStartMessage>
        {
            public Task Handle(MigrationStartMessage message, IMessageHandlerContext context)
            {
                throw new NotImplementedException();
            }

            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<NServiceBusMongoDBLegacySagaData> mapper)
            {
                mapper.ConfigureMapping<MigrationStartMessage>(msg => msg.Id).ToSaga(saga => saga.Id);
            }
        }

        class NServiceBusMongoDBLegacySagaData : IContainSagaData
        {
            public Guid Id { get; set; }

            public string OriginalMessageId { get; set; }

            public string Originator { get; set; }

            public Guid SomeCorrelationPropertyId { get; set; }

            public int SomeUpdatableSagaData { get; set; }
        }
    }
}

namespace NServiceBus.MongoDB
{
    class NServiceBusMongoDBLegacySagaData : IContainSagaData
    {
        public Guid Id { get; set; }

        public string OriginalMessageId { get; set; }

        public string Originator { get; set; }

        public int DocumentVersion { get; set; } //From NServiceBus.MongoDB.IHaveDocumentVersion

        public int ETag { get; set; } //From NServiceBus.MongoDB.IHaveDocumentVersion

        public Guid SomeCorrelationPropertyId { get; set; }

        public int SomeUpdatableSagaData { get; set; }
    }
}