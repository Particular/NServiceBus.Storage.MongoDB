using System;
using System.Threading.Tasks;
using MongoDB.Driver;
using NServiceBus;
using NServiceBus.AcceptanceTesting.Support;

class ConfigureEndpointMongoDBPersistence : IConfigureEndpointTestExecution
{
    public Task Configure(string endpointName, EndpointConfiguration configuration, RunSettings settings, PublisherMetadata publisherMetadata)
    {
        var containerConnectionString = Environment.GetEnvironmentVariable("ContainerUrl");

        var client = string.IsNullOrWhiteSpace(containerConnectionString) ? new MongoClient() : new MongoClient(containerConnectionString);

        configuration.UsePersistence<MongoDBPersistence>().Client(client);

        return Task.FromResult(0);
    }

    public Task Cleanup()
    {
        //TODO do we need cleanup?

        return Task.FromResult(0);
    }
}

