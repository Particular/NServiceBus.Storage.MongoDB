using System;
using System.Threading.Tasks;
using MongoDB.Driver;
using NServiceBus;
using NServiceBus.AcceptanceTesting.Support;

class ConfigureEndpointMongoDBPersistence : IConfigureEndpointTestExecution
{
    const string databaseName = "AcceptanceTests";
    static IMongoClient client;

    public Task Configure(string endpointName, EndpointConfiguration configuration, RunSettings settings, PublisherMetadata publisherMetadata)
    {
        var containerConnectionString = Environment.GetEnvironmentVariable("ContainerUrl");

        client = string.IsNullOrWhiteSpace(containerConnectionString) ? new MongoClient() : new MongoClient(containerConnectionString);

        configuration.UsePersistence<MongoDBPersistence>().Client(client).DatabaseName(databaseName);

        return Task.FromResult(0);
    }

    public Task Cleanup()
    {
        return client.DropDatabaseAsync(databaseName);
    }
}

