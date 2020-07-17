using System;

namespace NServiceBus.PersistenceTesting
{
    using System.Threading.Tasks;
    using NServiceBus.Outbox;
    using NServiceBus.Sagas;
    using Persistence;
    using Timeout.Core;
    using Unicast.Subscriptions.MessageDrivenSubscriptions;

    public class PersistenceTestsConfiguration : IPersistenceTestsConfiguration
    {
        public PersistenceTestsConfiguration()
        {
            SagaIdGenerator = new
        }

        public bool SupportsDtc => false;
        public bool SupportsOutbox => true;
        public bool SupportsFinders => true;
        public bool SupportsSubscriptions => true;
        public bool SupportsTimeouts => false;
        public bool SupportsOptimisticConcurrency => false;
        public bool SupportsPessimisticConcurrency => true;

        public ISagaIdGenerator SagaIdGenerator { get; }
        public ISagaPersister SagaStorage { get; }
        public ISynchronizedStorage SynchronizedStorage { get; }
        public ISynchronizedStorageAdapter SynchronizedStorageAdapter { get; }
        public ISubscriptionStorage SubscriptionStorage { get; }
        public IPersistTimeouts TimeoutStorage { get; }
        public IQueryTimeouts TimeoutQuery { get; }
        public IOutboxStorage OutboxStorage { get; }

        public Task Configure()
        {
            throw new NotImplementedException();
        }

        public Task Cleanup()
        {
            throw new NotImplementedException();
        }
    }
}