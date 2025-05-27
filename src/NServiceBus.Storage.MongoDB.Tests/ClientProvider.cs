namespace NServiceBus.Storage.MongoDB.Tests
{
    using System;
    using global::MongoDB.Bson;
    using global::MongoDB.Bson.Serialization;
    using global::MongoDB.Bson.Serialization.Serializers;
    using global::MongoDB.Driver;

    static class ClientProvider
    {
        public static IMongoClient Client
        {
            get
            {
                if (client == null)
                {
                    BsonSerializer.TryRegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));
                    var containerConnectionString = Environment.GetEnvironmentVariable("NServiceBusStorageMongoDB_ConnectionString");

                    client = string.IsNullOrWhiteSpace(containerConnectionString) ? new MongoClient() : new MongoClient(containerConnectionString);
                }

                return client;
            }
        }

        static IMongoClient client;
    }
}