#nullable enable

namespace NServiceBus.Storage.MongoDB.Tests;

using System;
using global::MongoDB.Driver;

static class ClientProvider
{
    public static IMongoClient Client
    {
        get
        {
            if (client != null)
            {
                return client;
            }

            MongoPersistence.SafeRegisterDefaultGuidSerializer();
            OutboxStorage.RegisterOutboxClassMappings();

            var containerConnectionString =
                Environment.GetEnvironmentVariable("NServiceBusStorageMongoDB_ConnectionString");

            client = string.IsNullOrWhiteSpace(containerConnectionString)
                ? new MongoClient()
                : new MongoClient(containerConnectionString);

            return client;
        }
    }

    static IMongoClient? client;
}