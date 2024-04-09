namespace NServiceBus.PersistenceTesting
{
    using System;
    using System.Threading.Tasks;
    using MongoDB.Bson;
    using MongoDB.Bson.Serialization;
    using MongoDB.Bson.Serialization.Serializers;
    using NUnit.Framework;
    using Sagas;

    public class When_finding_a_saga : SagaPersisterTests
    {
        [Test]
        public async Task Should_find_saga_with_custom_id()
        {
            BsonSerializer.RegisterSerializationProvider(new GuidSerializerProvider());

            var saga = new When_completing_a_saga_loaded_by_id.TestSagaData { SomeId = Guid.NewGuid().ToString(), DateTimeProperty = DateTime.UtcNow };
            await SaveSaga(saga);

            var context = configuration.GetContextBagForSagaStorage();
            using (var completeSession = configuration.CreateStorageSession())
            {
                await completeSession.Open(context);

                var sagaData = await configuration.SagaStorage.Get<When_completing_a_saga_loaded_by_id.TestSagaData>(saga.Id, completeSession, context);

                await configuration.SagaStorage.Complete(sagaData, completeSession, context);
                await completeSession.CompleteAsync();
            }

            var completedSaga = await GetById<When_completing_a_saga_loaded_by_id.TestSagaData>(saga.Id);
            Assert.Null(completedSaga);
        }

        public When_finding_a_saga(TestVariant param) : base(param)
        {
        }
    }

    public class GuidSerializerProvider : IBsonSerializationProvider
    {
        public IBsonSerializer GetSerializer(Type type)
        {
            if (type == typeof(Guid))
            {
                return new GuidSerializer(BsonType.String);
            }

            return null;
        }
    }
}