namespace NServiceBus.Storage.MongoDB.Tests
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using global::MongoDB.Bson;
    using global::MongoDB.Bson.Serialization;
    using global::MongoDB.Driver;
    using Persistence.ComponentTests;

    class SagaMigrationPersisterTests : SagaPersisterTests
    {
        protected Task PrepareSagaCollection<TSagaData>(TSagaData data, string correlationPropertyName) where TSagaData : IContainSagaData
        {
            return PrepareSagaCollection(data, correlationPropertyName, d => d.ToBsonDocument());
        }

        protected async Task PrepareSagaCollection<TSagaData>(TSagaData data, string correlationPropertyName, Func<TSagaData, BsonDocument> convertSagaData) where TSagaData : IContainSagaData
        {
            var sagaDataType = typeof(TSagaData);

            var document = convertSagaData(data);

            var collection = ClientProvider.Client.GetDatabase(configuration.DatabaseName).GetCollection<BsonDocument>(configuration.CollectionNamingConvention(sagaDataType));

            var propertyElementName = BsonClassMap.LookupClassMap(sagaDataType).AllMemberMaps.First(m => m.MemberName == correlationPropertyName).ElementName;

            var indexModel = new CreateIndexModel<BsonDocument>(new BsonDocumentIndexKeysDefinition<BsonDocument>(new BsonDocument(propertyElementName, 1)), new CreateIndexOptions
                {Unique = true});

            await collection.Indexes.CreateOneAsync(indexModel);

            await collection.InsertOneAsync(document);
        }
    }

    class MigrationStartMessage : ICommand
    {
        public Guid Id { get; set; }
    }
}