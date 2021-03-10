namespace NServiceBus.AcceptanceTests
{
    using NServiceBus.AcceptanceTesting.Support;

    public partial class TestSuiteConstraints
    {
        public bool SupportsDtc => false;

        public bool SupportsCrossQueueTransactions => true;

        public bool SupportsNativePubSub => false;

        public bool SupportsNativeDeferral => true;

        public bool SupportsOutbox => true;

        public bool SupportsPurgeOnStartup => true;

        public bool SupportsDelayedDelivery => true;

        public IConfigureEndpointTestExecution CreateTransportConfiguration() => new ConfigureEndpointAcceptanceTestingTransport(SupportsNativePubSub, SupportsNativeDeferral);

        public IConfigureEndpointTestExecution CreatePersistenceConfiguration() => new ConfigureEndpointMongoPersistence();
    }
}
