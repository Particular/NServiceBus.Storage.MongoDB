using System;
using System.Threading.Tasks;
using MongoDB.Driver;
using NServiceBus;
using NServiceBus.AcceptanceTesting.Support;

class ConfigureEndpointMongoDBPersistence : IConfigureEndpointTestExecution
{
    const string databaseName = "AcceptanceTests";
    IMongoClient client;

    public Task Configure(string endpointName, EndpointConfiguration configuration, RunSettings settings, PublisherMetadata publisherMetadata)
    {
        var containerConnectionString = Environment.GetEnvironmentVariable("NServiceBusStorageMongoDB_ConnectionString");

        client = string.IsNullOrWhiteSpace(containerConnectionString) ? new MongoClient() : new MongoClient(containerConnectionString);

        configuration.UsePersistence<MongoPersistence>().Client(client).DatabaseName(databaseName);

        return Task.FromResult(0);
    }

    public async Task Cleanup()
    {
        try
        {
            await client.DropDatabaseAsync(databaseName);
        }
        catch (Exception)
        { }
    }
}

