namespace NServiceBus.TransactionalSession.AcceptanceTests
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTesting.Customization;
    using NServiceBus.AcceptanceTesting.Support;
    using NUnit.Framework;

    public class TransactionSessionDefaultServer : IEndpointSetupTemplate
    {
        public virtual async Task<EndpointConfiguration> GetConfiguration(RunDescriptor runDescriptor, EndpointCustomizationConfiguration endpointCustomizations,
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

            endpointConfiguration.UseTransport(new AcceptanceTestingTransport
            {
                StorageLocation = storageDir
            });

            var mongoSettings = endpointConfiguration.UsePersistence<MongoPersistence>();
            mongoSettings.EnableTransactionalSession();
            mongoSettings.MongoClient(SetupFixture.MongoClient);
            mongoSettings.DatabaseName(SetupFixture.DatabaseName);
            mongoSettings.UseTransactions(true);

            endpointConfiguration.RegisterStartupTask(sp => new CaptureServiceProviderStartupTask(sp, runDescriptor.ScenarioContext));

            await configurationBuilderCustomization(endpointConfiguration).ConfigureAwait(false);

            // scan types at the end so that all types used by the configuration have been loaded into the AppDomain
            endpointConfiguration.TypesToIncludeInScan(endpointCustomizations.GetTypesScopedByTestClass());

            return endpointConfiguration;
        }
    }
}