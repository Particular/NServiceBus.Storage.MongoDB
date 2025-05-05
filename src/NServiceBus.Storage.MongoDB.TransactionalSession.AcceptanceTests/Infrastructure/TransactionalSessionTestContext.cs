namespace NServiceBus.TransactionalSession.AcceptanceTests;

using System;
using System.Collections.Concurrent;
using AcceptanceTesting;

public class TransactionalSessionTestContext : ScenarioContext
{
    public IServiceProvider ServiceProvider
    {
        get
        {
            var endpointName = GetCurrentEndpoint();

            if (!serviceProviders.TryGetValue(endpointName, out var serviceProvider))
            {
                throw new InvalidOperationException("Could not find service provider for endpoint " + endpointName);
            }

            return serviceProvider;
        }
    }

    public void RegisterServiceProvider(IServiceProvider serviceProvider)
    {
        var endpointName = GetCurrentEndpoint();

        serviceProviders[endpointName] = serviceProvider;
    }

    string GetCurrentEndpoint()
    {
        var endpointName = GetType().GetProperty("CurrentEndpoint")!.GetValue(this, null) as string;

        return endpointName;
    }

    readonly ConcurrentDictionary<string, IServiceProvider> serviceProviders = new();
}