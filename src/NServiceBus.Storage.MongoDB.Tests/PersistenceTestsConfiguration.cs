namespace NServiceBus.Persistence.ComponentTests
{
    using System;
    using System.Globalization;
    using System.Threading.Tasks;
    using Gateway.Deduplication;
    using NServiceBus.Storage.MongoDB;
    using NServiceBus.Storage.MongoDB.Tests;
    using Outbox;
    using Sagas;
    using Timeout.Core;
    using Unicast.Subscriptions.MessageDrivenSubscriptions;

    public partial class PersistenceTestsConfiguration
    {
        readonly string databaseName;
        readonly Func<Type, string> collectionNamingConvention;

        public PersistenceTestsConfiguration()
        {
            databaseName = "Test_" + DateTime.Now.Ticks.ToString(CultureInfo.InvariantCulture);
            collectionNamingConvention = t => t.Name.ToLower();

            var versionElementName = "_version";
            var useTransactions = true;

            SynchronizedStorage = new StorageSessionFactory(ClientProvider.Client, useTransactions, databaseName, collectionNamingConvention);
            SynchronizedStorageAdapter = new StorageSessionAdapter();

            SagaStorage = new SagaPersister(versionElementName);

            SagaIdGenerator = new DefaultSagaIdGenerator();

            OutboxStorage = new OutboxPersister(ClientProvider.Client, databaseName, collectionNamingConvention);
        }

        public bool SupportsDtc { get; } = false;

        public bool SupportsOutbox { get; } = true;

        public bool SupportsFinders { get; } = true;

        public bool SupportsSubscriptions { get; } = false;

        public bool SupportsTimeouts { get; } = false;

        public ISagaIdGenerator SagaIdGenerator { get; }

        public ISagaPersister SagaStorage { get; }

        public ISynchronizedStorage SynchronizedStorage { get; }

        public ISynchronizedStorageAdapter SynchronizedStorageAdapter { get; }

        public ISubscriptionStorage SubscriptionStorage { get; }

        public IPersistTimeouts TimeoutStorage { get; }

        public IQueryTimeouts TimeoutQuery { get; }

        public IOutboxStorage OutboxStorage { get; }

        public IDeduplicateMessages GatewayStorage { get; }

        public Task Configure()
        {
            var database = ClientProvider.Client.GetDatabase(databaseName);

            Storage.MongoDB.SagaStorage.CreateIndexes(database, collectionNamingConvention, SagaMetadataCollection);

            return Task.FromResult(0);
        }

        public async Task Cleanup()
        {
            await ClientProvider.Client.DropDatabaseAsync(databaseName);
        }

        public Task CleanupMessagesOlderThan(DateTimeOffset beforeStore)
        {
            return Task.FromResult(0);
        }

        class DefaultSagaIdGenerator : ISagaIdGenerator
        {
            public Guid Generate(SagaIdGeneratorContext context)
            {
                // intentionally ignore the property name and the value.
                return CombGuid.Generate();
            }
        }
    }
}