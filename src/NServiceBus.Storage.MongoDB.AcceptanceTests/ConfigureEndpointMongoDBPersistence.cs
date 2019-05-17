﻿using System;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.AcceptanceTesting.Support;

class ConfigureEndpointMongoDBPersistence : IConfigureEndpointTestExecution
{
    public Task Configure(string endpointName, EndpointConfiguration configuration, RunSettings settings, PublisherMetadata publisherMetadata)
    {
        var containerConnectionString = Environment.GetEnvironmentVariable("ContainerUrl");
        //TODO better error handling for connection string?

        configuration.UsePersistence<MongoDBPersistence>().ConnectionString(containerConnectionString);

        return Task.FromResult(0);
    }

    public Task Cleanup()
    {
        //TODO do we need cleanup?

        return Task.FromResult(0);
    }
}
