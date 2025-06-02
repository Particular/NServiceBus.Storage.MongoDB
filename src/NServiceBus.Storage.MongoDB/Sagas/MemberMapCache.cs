namespace NServiceBus.Storage.MongoDB;

using System;
using System.Collections.Concurrent;
using global::MongoDB.Bson.Serialization;
using Sagas;

sealed class MemberMapCache
{
    public BsonMemberMap GetOrAdd<TSagaData>(SagaMetadata.CorrelationPropertyMetadata metadata)
        where TSagaData : class, IContainSagaData
        => GetOrAdd(typeof(TSagaData), metadata);

    public BsonMemberMap GetOrAdd(Type sagaDataType, SagaMetadata.CorrelationPropertyMetadata metadata)
        => GetOrAdd(sagaDataType, metadata.Name);

    public BsonMemberMap GetOrAdd<TSagaData>(string propertyName)
        where TSagaData : class, IContainSagaData
        => GetOrAdd(typeof(TSagaData), propertyName);

    public BsonMemberMap GetOrAdd(Type sagaDataType, string propertyName) =>
        typeAndPropertyToMemberMapCache.GetOrAdd((sagaDataType, propertyName), static key =>
        {
            (Type type, string propertyName) = key;

            var classMap = BsonClassMap.LookupClassMap(type);

            foreach (var memberMap in classMap.AllMemberMaps)
            {
                if (memberMap.MemberName == propertyName)
                {
                    return memberMap;
                }
            }

            throw new InvalidOperationException($"Property '{propertyName}' not found in '{type}' class map.");
        });

    readonly ConcurrentDictionary<(Type, string), BsonMemberMap> typeAndPropertyToMemberMapCache = new();
}