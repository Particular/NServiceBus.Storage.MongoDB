namespace NServiceBus.Storage.MongoDB.Tests
{
    using System;
    using global::MongoDB.Driver;

    static class ClientProvider
    {
        static IMongoClient client;

        public static IMongoClient Client
        {
            get
            {
                if (client == null)
                {
                    var containerConnectionString = Environment.GetEnvironmentVariable("ContainerUrl");

                    client = string.IsNullOrWhiteSpace(containerConnectionString) ? new MongoClient() : new MongoClient(containerConnectionString);
                }

                return client;
            }
        }
    }
}