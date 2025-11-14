namespace NServiceBus.TransactionalSession.AcceptanceTests;

using System;
using System.IO;
using System.Threading.Tasks;
using AcceptanceTesting;
using AcceptanceTesting.Customization;
using AcceptanceTesting.Support;
using Configuration.AdvancedExtensibility;
using NUnit.Framework;

public class DefaultServer : IEndpointSetupTemplate
{
    public virtual async Task<EndpointConfiguration> GetConfiguration(RunDescriptor runDescriptor,
        EndpointCustomizationConfiguration endpointCustomizations,
        Func<EndpointConfiguration, Task> configurationBuilderCustomization)
    {
        var endpointConfiguration = new EndpointConfiguration(endpointCustomizations.EndpointName);

        endpointConfiguration.EnableInstallers();
        endpointConfiguration.UseSerialization<SystemJsonSerializer>();
        endpointConfiguration.Recoverability()
            .Delayed(delayed => delayed.NumberOfRetries(0))
            .Immediate(immediate => immediate.NumberOfRetries(0));
        endpointConfiguration.SendFailedMessagesTo("error");

        var storageDir = Path.Combine(Path.GetTempPath(), "learn", TestContext.CurrentContext.Test.ID);

        endpointConfiguration.UseTransport(new AcceptanceTestingTransport { StorageLocation = storageDir });

        var persistence = endpointConfiguration.UsePersistence<MongoPersistence>();
        persistence.EnableTransactionalSession();
        persistence.MongoClient(SetupFixture.MongoClient);
        persistence.DatabaseName(SetupFixture.DatabaseName);
        persistence.UseTransactions(true);

        endpointConfiguration.GetSettings().Set(persistence);

        if (runDescriptor.ScenarioContext is TransactionalSessionTestContext testContext)
        {
            endpointConfiguration.RegisterStartupTask(sp =>
                new CaptureServiceProviderStartupTask(sp, testContext, endpointCustomizations.EndpointName));
        }

        await configurationBuilderCustomization(endpointConfiguration).ConfigureAwait(false);

        // scan types at the end so that all types used by the configuration have been loaded into the AppDomain
        endpointConfiguration.ScanTypesForTest(endpointCustomizations);

        return endpointConfiguration;
    }
}