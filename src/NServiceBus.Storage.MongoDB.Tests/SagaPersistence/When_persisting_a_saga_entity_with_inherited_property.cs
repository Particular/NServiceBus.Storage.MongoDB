using System;
using System.Threading.Tasks;
using NUnit.Framework;

namespace NServiceBus.Storage.MongoDB.Tests.SagaPersistence
{
    public class When_persisting_a_saga_entity_with_inherited_property : MongoFixture
    {
        TestSaga entity;
        TestSaga savedEntity;

        [SetUp]
        public async Task Setup()
        {
            entity = new TestSaga
            {
                Id = Guid.NewGuid(),
                PolymorpicRelatedProperty = new PolymorpicProperty {SomeInt = 9}
            };

            await SaveSaga(entity).ConfigureAwait(false);

            savedEntity = await LoadSaga<TestSaga>(entity.Id).ConfigureAwait(false);
        }

        [Test]
        public void Inherited_property_classes_should_be_persisted()
        {
            Assert.AreEqual(entity.PolymorpicRelatedProperty, savedEntity.PolymorpicRelatedProperty);
        }
    }
}