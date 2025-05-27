namespace NServiceBus.Storage.MongoDB
{
    using System;
    using global::MongoDB.Bson.Serialization;
    using Sagas;

    static class TypeExtensions
    {
        public static BsonMemberMap GetMemberMap(this Type t, SagaMetadata.CorrelationPropertyMetadata metadata)
            => t.GetMemberMap(new Property(metadata.Name));

        public static BsonMemberMap GetMemberMap(this Type type, Property property)
        {
            var classMap = BsonClassMap.LookupClassMap(type);

            foreach (var memberMap in classMap.AllMemberMaps)
            {
                if (memberMap.MemberName == property.Name)
                {
                    return memberMap;
                }
            }

            throw new InvalidOperationException($"Property '{property.Name}' not found in '{type}' class map.");
        }

        public static BsonMemberMap GetMemberMap(this Type type, Element element)
        {
            var classMap = BsonClassMap.LookupClassMap(type);

            foreach (var memberMap in classMap.AllMemberMaps)
            {
                if (memberMap.ElementName == element.Name)
                {
                    return memberMap;
                }
            }

            throw new InvalidOperationException($"Element '{element.Name}' not found in '{type}' class map.");
        }
    }

    readonly record struct Property(string Name);
    readonly record struct Element(string Name);
}