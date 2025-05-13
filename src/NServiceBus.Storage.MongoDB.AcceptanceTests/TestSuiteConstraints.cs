namespace NServiceBus.AcceptanceTests
{
    using NServiceBus.AcceptanceTesting.Support;

    public partial class TestSuiteConstraints
    {
        public bool SupportsDtc => false;

        public bool SupportsCrossQueueTransactions => true;

        //The persister has a subscription storage so we set this to false to make sure it will be used in the tests
        public bool SupportsNativePubSub => false;

        public bool SupportsNativeDeferral => true;

        public bool SupportsOutbox => true;

        public bool SupportsPurgeOnStartup => true;

        public bool SupportsDelayedDelivery => true;

        public IConfigureEndpointTestExecution CreateTransportConfiguration() => new ConfigureEndpointAcceptanceTestingTransport(SupportsNativePubSub, SupportsNativeDeferral);

        public IConfigureEndpointTestExecution CreatePersistenceConfiguration() => new ConfigureEndpointMongoPersistence();
    }
}