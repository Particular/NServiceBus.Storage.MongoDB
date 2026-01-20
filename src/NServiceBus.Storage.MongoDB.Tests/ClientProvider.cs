namespace NServiceBus.Storage.MongoDB.Tests
{
    using System;
    using global::MongoDB.Driver;

    static class ClientProvider
    {
        public static IMongoClient Client
        {
#if NET10_0_OR_GREATER
            get
            {
                if (field == null)
                {
                    var containerConnectionString = Environment.GetEnvironmentVariable("NServiceBusStorageMongoDB_ConnectionString");

                    field = string.IsNullOrWhiteSpace(containerConnectionString) ? new MongoClient() : new MongoClient(containerConnectionString);
                }

                return field;
            }
#else
            get
            {
                if (client == null)
                {
                    var containerConnectionString = Environment.GetEnvironmentVariable("NServiceBusStorageMongoDB_ConnectionString");
                    client = string.IsNullOrWhiteSpace(containerConnectionString) ? new MongoClient() : new MongoClient(containerConnectionString);
                }
                return client;
            }
#endif
        }

#if !NET10_0_OR_GREATER
        static IMongoClient client;
#endif
    }
}