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
        public virtual async Task<EndpointConfiguration> GetConfiguration(RunDescriptor runDescriptor, EndpointCustomizationConfiguration endpointConfiguration,
            Func<EndpointConfiguration, Task> configurationBuilderCustomization)
        {
            var builder = new EndpointConfiguration(endpointConfiguration.EndpointName);
            builder.EnableInstallers();

            builder.Recoverability()
                .Delayed(delayed => delayed.NumberOfRetries(0))
                .Immediate(immediate => immediate.NumberOfRetries(0));
            builder.SendFailedMessagesTo("error");

            var storageDir = Path.Combine(Path.GetTempPath(), "learn", TestContext.CurrentContext.Test.ID);

            builder.UseTransport(new AcceptanceTestingTransport
            {
                StorageLocation = storageDir
            });

            var mongoSettings = builder.UsePersistence<MongoPersistence>();
            mongoSettings.EnableTransactionalSession();
            mongoSettings.MongoClient(SetupFixture.MongoClient);
            mongoSettings.DatabaseName(SetupFixture.DatabaseName);
            mongoSettings.UseTransactions(true);

            builder.RegisterStartupTask(sp => new CaptureServiceProviderStartupTask(sp, runDescriptor.ScenarioContext));

            await configurationBuilderCustomization(builder).ConfigureAwait(false);

            // scan types at the end so that all types used by the configuration have been loaded into the AppDomain
            builder.TypesToIncludeInScan(endpointConfiguration.GetTypesScopedByTestClass());

            return builder;
        }
    }
}