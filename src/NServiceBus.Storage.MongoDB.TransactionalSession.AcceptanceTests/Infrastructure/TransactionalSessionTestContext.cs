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
            var endpointName = GetType().GetProperty("CurrentEndpoint")!.GetValue(this, null) as string;

            if (!serviceProviders.TryGetValue(endpointName, out var serviceProvider))
            {
                throw new InvalidOperationException("Could not find service provider for endpoint " + endpointName);
            }

            return serviceProvider;
        }
    }

    public void RegisterServiceProvider(IServiceProvider serviceProvider, string endpointName) => serviceProviders[endpointName] = serviceProvider;

    readonly ConcurrentDictionary<string, IServiceProvider> serviceProviders = new();
}