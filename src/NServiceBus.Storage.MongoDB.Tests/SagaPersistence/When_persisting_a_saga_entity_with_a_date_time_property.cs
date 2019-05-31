using System;
using System.Threading.Tasks;
using NUnit.Framework;

namespace NServiceBus.Storage.MongoDB.Tests.SagaPersistence
{
    public class When_persisting_a_saga_entity_with_a_DateTime_property : MongoFixture
    {
        TestSaga entity;
        TestSaga savedEntity;

        [SetUp]
        public async Task Setup()
        {
            entity = new TestSaga
            {
                Id = Guid.NewGuid(),
                DateTimeProperty = DateTime.Parse("12/02/2010 12:00:00.01").ToUniversalTime()
            };

            await SaveSaga(entity).ConfigureAwait(false);

            savedEntity = await LoadSaga<TestSaga>(entity.Id).ConfigureAwait(false);
        }

        [Test]
        public void Datetime_property_should_be_persisted()
        {
            Assert.AreEqual(entity.DateTimeProperty, savedEntity.DateTimeProperty);
        }
    }
}