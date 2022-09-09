using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using MongoDB.Driver;
using MongoDB.Driver.Core.Events;
using NServiceBus;
using NServiceBus.AcceptanceTesting.Support;
using NServiceBus.Configuration.AdvancedExtensibility;

class ConfigureEndpointMongoPersistence : IConfigureEndpointTestExecution
{
    public const string DatabaseName = "AcceptanceTests";
    public const string InterceptedCommands = "MongoDB.AcceptanceTests.InterceptedCommands";
    IMongoClient client;

    public Task Configure(string endpointName, EndpointConfiguration configuration, RunSettings settings, PublisherMetadata publisherMetadata)
    {
        var containerConnectionString = Environment.GetEnvironmentVariable("NServiceBusStorageMongoDB_ConnectionString");

        var commands = new ConcurrentQueue<string>();
        configuration.GetSettings().Set(InterceptedCommands, commands);

        var mongoClientSettings = string.IsNullOrWhiteSpace(containerConnectionString)
            ? new MongoClientSettings()
            : MongoClientSettings.FromConnectionString(containerConnectionString);
        mongoClientSettings.ClusterConfigurator = cb =>
        {
            cb.Subscribe<CommandSucceededEvent>(commandSucceededEvent =>
            {
                commands.Enqueue($"{commandSucceededEvent.RequestId}-{commandSucceededEvent.CommandName.ToUpper()}");
            });
        };

        client = new MongoClient(mongoClientSettings);

        configuration.UsePersistence<MongoPersistence>().MongoClient(client).DatabaseName(DatabaseName);

        return Task.FromResult(0);
    }

    public async Task Cleanup()
    {
        try
        {
            await client.DropDatabaseAsync(DatabaseName);
        }
        catch (Exception)
        { }
    }
}