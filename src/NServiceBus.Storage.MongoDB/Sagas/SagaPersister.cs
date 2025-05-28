namespace NServiceBus.Storage.MongoDB;

using System;
using System.Threading;
using System.Threading.Tasks;
using Extensibility;
using global::MongoDB.Bson;
using global::MongoDB.Bson.Serialization;
using global::MongoDB.Driver;
using Persistence;
using Sagas;

class SagaPersister(string versionElementName, MemberMapCache memberMapCache) : ISagaPersister
{
    public async Task Save(IContainSagaData sagaData, SagaCorrelationProperty correlationProperty,
        ISynchronizedStorageSession session, ContextBag context, CancellationToken cancellationToken = default)
    {
        var storageSession = ((SynchronizedStorageSession)session).Session!;
        var sagaDataType = sagaData.GetType();

        var document = sagaData.ToBsonDocument(sagaDataType);
        document.Add(versionElementName, 0);

        await storageSession.InsertOneAsync(sagaDataType, document, cancellationToken).ConfigureAwait(false);
    }

    public async Task Update(IContainSagaData sagaData, ISynchronizedStorageSession session, ContextBag context,
        CancellationToken cancellationToken = default)
    {
        var storageSession = ((SynchronizedStorageSession)session).Session!;
        var sagaDataType = sagaData.GetType();

        var version = storageSession.RetrieveVersion(sagaDataType);
        var document = sagaData.ToBsonDocument(sagaDataType)
            .SetElement(new BsonElement(versionElementName, version + 1));

        var memberMap = memberMapCache.GetOrAdd(sagaDataType, nameof(IContainSagaData.Id));
        var serializer = memberMap.GetSerializer();
        var serializedElementValue = serializer.ToBsonValue(sagaData.Id);

        var result = await storageSession.ReplaceOneAsync(sagaDataType,
            new BsonDocument(memberMap.ElementName, serializedElementValue) &
            filterBuilder.Eq(versionElementName, version), document, cancellationToken).ConfigureAwait(false);

        if (result.ModifiedCount != 1)
        {
            throw new Exception(
                $"The '{sagaDataType.Name}' saga with id '{sagaData.Id}' was updated by another process or no longer exists.");
        }
    }

    public Task<TSagaData> Get<TSagaData>(Guid sagaId, ISynchronizedStorageSession session, ContextBag context,
        CancellationToken cancellationToken = default) where TSagaData : class, IContainSagaData =>
        GetSagaData<TSagaData>(memberMapCache.GetOrAdd<TSagaData>(nameof(IContainSagaData.Id)), sagaId, session,
            cancellationToken);

    public Task<TSagaData> Get<TSagaData>(string propertyName, object propertyValue,
        ISynchronizedStorageSession session, ContextBag context, CancellationToken cancellationToken = default)
        where TSagaData : class, IContainSagaData =>
        GetSagaData<TSagaData>(memberMapCache.GetOrAdd<TSagaData>(propertyName), propertyValue, session,
            cancellationToken);

    public async Task Complete(IContainSagaData sagaData, ISynchronizedStorageSession session, ContextBag context,
        CancellationToken cancellationToken = default)
    {
        var storageSession = ((SynchronizedStorageSession)session).Session!;
        var sagaDataType = sagaData.GetType();

        var version = storageSession.RetrieveVersion(sagaDataType);

        var memberMap = memberMapCache.GetOrAdd(sagaDataType, nameof(IContainSagaData.Id));
        var serializer = memberMap.GetSerializer();
        var serializedElementValue = serializer.ToBsonValue(sagaData.Id);

        var result = await storageSession.DeleteOneAsync(sagaDataType,
            new BsonDocument(memberMap.ElementName, serializedElementValue) &
            filterBuilder.Eq(versionElementName, version), cancellationToken).ConfigureAwait(false);

        if (result.DeletedCount != 1)
        {
            throw new Exception("Saga can't be completed because it was updated by another process.");
        }
    }

    async Task<TSagaData> GetSagaData<TSagaData>(BsonMemberMap memberMap, object elementValue,
        ISynchronizedStorageSession session, CancellationToken cancellationToken)
    {
        var storageSession = ((SynchronizedStorageSession)session).Session!;

        var serializer = memberMap.GetSerializer();
        var serializedElementValue = serializer.ToBsonValue(elementValue);
        var document = await storageSession
            .Find<TSagaData>(new BsonDocument(memberMap.ElementName, serializedElementValue), cancellationToken)
            .ConfigureAwait(false);

        if (document is not null)
        {
            var version = document.GetValue(versionElementName);
            storageSession.StoreVersion<TSagaData>(version.AsInt32);

            return BsonSerializer.Deserialize<TSagaData>(document);
        }

        return default!;
    }

    readonly FilterDefinitionBuilder<BsonDocument> filterBuilder = Builders<BsonDocument>.Filter;

    internal const string DefaultVersionElementName = "_version";
}