namespace NServiceBus.TransactionalSession.AcceptanceTests;

using System;
using System.Collections.Concurrent;
using System.Reflection;
using AcceptanceTesting;

public class TransactionalSessionTestContext : ScenarioContext
{
    public IServiceProvider ServiceProvider
    {
        get
        {
            var property = typeof(ScenarioContext).GetProperty("CurrentEndpoint", BindingFlags.NonPublic | BindingFlags.Static);
            var endpointName = property!.GetValue(this) as string;

            ArgumentException.ThrowIfNullOrEmpty(endpointName);

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