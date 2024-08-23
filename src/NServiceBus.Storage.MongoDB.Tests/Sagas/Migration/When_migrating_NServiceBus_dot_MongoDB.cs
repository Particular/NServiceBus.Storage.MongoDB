namespace NServiceBus.Storage.MongoDB.Tests
{
    using System;
    using System.Threading.Tasks;
    using global::MongoDB.Bson;
    using NUnit.Framework;

    class When_migrating_NServiceBus_dot_MongoDB : SagaMigrationPersisterTests
    {
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

            configuration = new SagaTestsConfiguration(nameof(legacySagaData.DocumentVersion), collectionNamingConvention);

            await PrepareSagaCollection(legacySagaData, nameof(legacySagaData.SomeCorrelationPropertyId), d =>
            {
                var document = d.ToBsonDocument();

                d.ETag = document.GetHashCode();

                return d.ToBsonDocument();
            });

            var retrievedSagaData = await GetById<NServiceBusMongoDBLegacySagaData>(legacySagaData.Id);

            Assert.That(retrievedSagaData, Is.Not.Null, "Saga was not retrieved");
            Assert.That(retrievedSagaData.OriginalMessageId, Is.EqualTo(legacySagaData.OriginalMessageId), "OriginalMessageId does not match");
            Assert.That(retrievedSagaData.Originator, Is.EqualTo(legacySagaData.Originator), "Originator does not match");
            Assert.That(retrievedSagaData.SomeCorrelationPropertyId, Is.EqualTo(legacySagaData.SomeCorrelationPropertyId), "SomeCorrelationPropertyId does not match");
            Assert.That(retrievedSagaData.SomeUpdatableSagaData, Is.EqualTo(legacySagaData.SomeUpdatableSagaData), "SomeUpdatableSagaData does not match");
        }

        readonly Func<Type, string> collectionNamingConvention = t => t.Name;

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
            public Guid SomeCorrelationPropertyId { get; set; }

            public int SomeUpdatableSagaData { get; set; }
            public Guid Id { get; set; }

            public string OriginalMessageId { get; set; }

            public string Originator { get; set; }
        }
    }
}

namespace NServiceBus.MongoDB
{
    using System;

    class NServiceBusMongoDBLegacySagaData : IContainSagaData
    {
        public int DocumentVersion { get; set; } //From NServiceBus.MongoDB.IHaveDocumentVersion

        public int ETag { get; set; } //From NServiceBus.MongoDB.IHaveDocumentVersion

        public Guid SomeCorrelationPropertyId { get; set; }

        public int SomeUpdatableSagaData { get; set; }
        public Guid Id { get; set; }

        public string OriginalMessageId { get; set; }

        public string Originator { get; set; }
    }
}