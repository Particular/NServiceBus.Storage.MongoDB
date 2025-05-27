namespace NServiceBus.Storage.MongoDB
{
    using System;
    using System.Collections.Concurrent;
    using global::MongoDB.Bson.Serialization;
    using Sagas;

    static class TypeExtensions
    {
        public static BsonMemberMap GetMemberMap(this Type t, SagaMetadata.CorrelationPropertyMetadata metadata)
            => t.GetMemberMap(metadata.Name);

        public static BsonMemberMap GetMemberMap(this Type type, string propertyName) =>
            typeAndPropertyToMemberMapCache.GetOrAdd((type, propertyName), static key =>
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

        static readonly ConcurrentDictionary<(Type, string), BsonMemberMap> typeAndPropertyToMemberMapCache = new();
    }
}