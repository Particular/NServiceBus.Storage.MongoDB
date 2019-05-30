using System;
using MongoDB.Driver;

namespace NServiceBus.Storage.MongoDB.Tests
{
    static class ClientProvider
    {
        static IMongoClient client;

        public static IMongoClient Client
        {
            get
            {
                if (client == null)
                {
                    var containerConnectionString = Environment.GetEnvironmentVariable("NServiceBusStorageMongoDB_ConnectionString");

                    client = string.IsNullOrWhiteSpace(containerConnectionString) ? new MongoClient() : new MongoClient(containerConnectionString);
                }

                return client;
            }
        }
    }
}