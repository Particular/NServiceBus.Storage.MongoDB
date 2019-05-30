using System;
using System.Threading.Tasks;
using NUnit.Framework;

namespace NServiceBus.Storage.MongoDB.Tests.SagaPersistence
{
    public class When_persisting_a_saga_entity_with_related_entities : MongoFixture
    {
        RelatedClass relatedClass;
        TestSaga savedEntity;

        [SetUp]
        public async Task Setup()
        {
            var entity = new TestSaga {Id = Guid.NewGuid(), RelatedClass = new RelatedClass {Id = Guid.NewGuid()}};
            relatedClass = entity.RelatedClass;

            await SaveSaga(entity).ConfigureAwait(false);

            savedEntity = await LoadSaga<TestSaga>(entity.Id).ConfigureAwait(false);
        }


        [Test]
        public void Related_entities_should_also_be_persisted()
        {
            Assert.AreEqual(relatedClass.Id, savedEntity.RelatedClass.Id);
        }
    }
}