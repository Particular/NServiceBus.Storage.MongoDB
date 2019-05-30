using System;
using System.Threading.Tasks;
using NUnit.Framework;

namespace NServiceBus.Storage.MongoDB.Tests.SagaPersistence
{
    public class When_persisting_a_saga_entity_with_a_concrete_class_property : MongoFixture
    {
        TestSaga entity;
        TestSaga savedEntity;

        [SetUp]
        public async Task Setup()
        {
            entity = new TestSaga {Id = Guid.NewGuid(), TestComponent = new TestComponent {Property = "Prop"}};

            await SaveSaga(entity).ConfigureAwait(false);

            savedEntity = await LoadSaga<TestSaga>(entity.Id).ConfigureAwait(false);
        }

        [Test]
        public void Public_setters_and_getters_of_concrete_classes_should_be_persisted()
        {
            Assert.AreEqual(entity.TestComponent, savedEntity.TestComponent);
        }
    }
}