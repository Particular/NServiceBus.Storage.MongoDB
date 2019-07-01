using System;
using System.Globalization;
using System.Threading.Tasks;
using NServiceBus.Gateway.Deduplication;
using NServiceBus.Outbox;
using NServiceBus.Sagas;
using NServiceBus.Storage.MongoDB;
using NServiceBus.Storage.MongoDB.Tests;
using NServiceBus.Timeout.Core;
using NServiceBus.Unicast.Subscriptions.MessageDrivenSubscriptions;

namespace NServiceBus.Persistence.ComponentTests
{
    public partial class PersistenceTestsConfiguration
    {
        public string DatabaseName { get; }

        public Func<Type, string> CollectionNamingConvention { get; }

        public PersistenceTestsConfiguration(string versionElementName, Func<Type, string> collectionNamingConvention)
        {
            DatabaseName = "Test_" + DateTime.Now.Ticks.ToString(CultureInfo.InvariantCulture);
            CollectionNamingConvention = collectionNamingConvention;

            var useTransactions = true;

            SynchronizedStorage = new StorageSessionFactory(ClientProvider.Client, useTransactions, DatabaseName, collectionNamingConvention);
            SynchronizedStorageAdapter = new StorageSessionAdapter();

            SagaIdGenerator = new DefaultSagaIdGenerator();
            SagaStorage = new SagaPersister(versionElementName);

            OutboxStorage = new OutboxPersister(ClientProvider.Client, DatabaseName, collectionNamingConvention);
        }

        public PersistenceTestsConfiguration() : this("_version", t => t.Name.ToLower())
        {
        }

        public PersistenceTestsConfiguration(string versionElementName) : this(versionElementName, t => t.Name.ToLower())
        {
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

        public async Task Configure()
        {
            var database = ClientProvider.Client.GetDatabase(DatabaseName);

            await database.CreateCollectionAsync(CollectionNamingConvention(typeof(OutboxRecord)));

            Storage.MongoDB.SagaStorage.CreateIndexes(database, CollectionNamingConvention, SagaMetadataCollection);
        }

        public async Task Cleanup()
        {
            await ClientProvider.Client.DropDatabaseAsync(DatabaseName);
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