using System;
using System.Threading.Tasks;
using NUnit.Framework;

namespace NServiceBus.Storage.MongoDB.Tests.SagaPersistence
{
    public class When_persisting_a_saga_entity_with_an_Enum_property : MongoFixture
    {
        TestSaga entity;
        TestSaga savedEntity;

        [SetUp]
        public async Task Setup()
        {
            entity = new TestSaga {Id = Guid.NewGuid(), Status = StatusEnum.AnotherStatus};

            await SaveSaga(entity).ConfigureAwait(false);

            savedEntity = await LoadSaga<TestSaga>(entity.Id).ConfigureAwait(false);
        }

        [Test]
        public void Enums_should_be_persisted()
        {
            Assert.AreEqual(entity.Status, savedEntity.Status);
        }
    }
}