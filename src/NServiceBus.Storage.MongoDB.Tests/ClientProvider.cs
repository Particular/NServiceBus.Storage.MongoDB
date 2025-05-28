namespace NServiceBus.Storage.MongoDB.Tests
{
    using System;
    using global::MongoDB.Driver;

    static class ClientProvider
    {
        public static IMongoClient Client
        {
            get
            {
                if (client == null)
                {
                    MongoPersistence.SafeRegisterDefaultGuidSerializer();

                    var containerConnectionString = Environment.GetEnvironmentVariable("NServiceBusStorageMongoDB_ConnectionString");

                    client = string.IsNullOrWhiteSpace(containerConnectionString) ? new MongoClient() : new MongoClient(containerConnectionString);
                }

                return client;
            }
        }

        static IMongoClient client;
    }
}