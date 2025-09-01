namespace NServiceBus.Storage.MongoDB.Tests;

using System;
using global::MongoDB.Driver;

static class ClientProvider
{
    public static IMongoClient Client
    {
        get
        {
            if (field != null)
            {
                return field;
            }

            MongoPersistence.SafeRegisterDefaultGuidSerializer();
            OutboxStorage.RegisterOutboxClassMappings();

            var containerConnectionString =
                Environment.GetEnvironmentVariable("NServiceBusStorageMongoDB_ConnectionString");

            field = string.IsNullOrWhiteSpace(containerConnectionString)
                ? new MongoClient()
                : new MongoClient(containerConnectionString);

            return field;
        }
    }
}