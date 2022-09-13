namespace NServiceBus.Persistence.ComponentTests
{
    using System;
    using System.Globalization;
    using System.Threading.Tasks;
    using Gateway.Deduplication;
    using global::MongoDB.Driver;
    using Outbox;
    using Sagas;
    using Storage.MongoDB;
    using Storage.MongoDB.Tests;
    using Timeout.Core;
    using Unicast.Subscriptions.MessageDrivenSubscriptions;

    public partial class PersistenceTestsConfiguration
    {
        public PersistenceTestsConfiguration(string versionElementName, Func<Type, string> collectionNamingConvention, TimeSpan? transactionTimeout = null)
        {
            DatabaseName = "Test_" + DateTime.Now.Ticks.ToString(CultureInfo.InvariantCulture);
            CollectionNamingConvention = collectionNamingConvention;

            SynchronizedStorage = new StorageSessionFactory(ClientProvider.Client, true, DatabaseName, collectionNamingConvention, transactionTimeout.HasValue ? transactionTimeout.Value : MongoPersistence.DefaultTransactionTimeout);
            SynchronizedStorageAdapter = new StorageSessionAdapter();

            SagaIdGenerator = new DefaultSagaIdGenerator();
            SagaStorage = new SagaPersister(versionElementName);

            OutboxStorage = new OutboxPersister(ClientProvider.Client, DatabaseName, collectionNamingConvention);

            var subscriptionCollection = ClientProvider.Client.GetDatabase(DatabaseName, MongoPersistence.DefaultDatabaseSettings).GetCollection<EventSubscription>("eventsubscriptions");
            var subscriptionPersister = new SubscriptionPersister(subscriptionCollection);
            subscriptionPersister.CreateIndexes();
            SubscriptionStorage = subscriptionPersister;
        }

        public PersistenceTestsConfiguration(TimeSpan? transactionTimeout = null) : this("_version", t => t.Name.ToLower(), transactionTimeout)
        {
        }

        public PersistenceTestsConfiguration(string versionElementName) : this(versionElementName, t => t.Name.ToLower())
        {
        }

        public string DatabaseName { get; }

        public Func<Type, string> CollectionNamingConvention { get; }

        public bool SupportsDtc { get; } = false;

        public bool SupportsOutbox { get; } = true;

        public bool SupportsFinders { get; } = true;

        public bool SupportsSubscriptions { get; } = true;

        public bool SupportsTimeouts { get; } = false;

        public bool SupportsPessimisticConcurrency { get; } = true;

        public ISagaIdGenerator SagaIdGenerator { get; }

        public ISagaPersister SagaStorage { get; }

        public ISynchronizedStorage SynchronizedStorage { get; }

        public ISynchronizedStorageAdapter SynchronizedStorageAdapter { get; }

        public ISubscriptionStorage SubscriptionStorage { get; }

        public IPersistTimeouts TimeoutStorage { get; }

        public IQueryTimeouts TimeoutQuery { get; }

        public IOutboxStorage OutboxStorage { get; }

#pragma warning disable CS0618
        public IDeduplicateMessages GatewayStorage { get; }
#pragma warning restore CS0618

        public async Task Configure()
        {
            var databaseSettings = new MongoDatabaseSettings
            {
                ReadConcern = ReadConcern.Majority,
                ReadPreference = ReadPreference.Primary,
                WriteConcern = WriteConcern.WMajority
            };

            var database = ClientProvider.Client.GetDatabase(DatabaseName, databaseSettings);

            await database.CreateCollectionAsync(CollectionNamingConvention(typeof(OutboxRecord)));

            Storage.MongoDB.SagaStorage.InitializeSagaDataTypes(database, CollectionNamingConvention, SagaMetadataCollection);
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