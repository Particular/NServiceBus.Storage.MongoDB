namespace NServiceBus.PersistenceTesting
{
    using System;
    using System.Globalization;
    using System.Threading.Tasks;
    using MongoDB.Driver;
    using NServiceBus.Outbox;
    using NServiceBus.Sagas;
    using Persistence;
    using Persistence.ComponentTests;
    using Storage.MongoDB;
    using Storage.MongoDB.Tests;

    public partial class PersistenceTestsConfiguration
    {
        public bool SupportsDtc => false;
        public bool SupportsOutbox => true;
        public bool SupportsFinders => false;
        public bool SupportsSubscriptions => true;
        public bool SupportsTimeouts => false;
        public bool SupportsPessimisticConcurrency => true;

        public ISagaIdGenerator SagaIdGenerator { get; } = new DefaultSagaIdGenerator();
        public ISagaPersister SagaStorage { get; private set; }
        public ISynchronizedStorage SynchronizedStorage { get; private set; }
        public ISynchronizedStorageAdapter SynchronizedStorageAdapter { get; private set; }
        public IOutboxStorage OutboxStorage { get; private set; }

        public async Task Configure()
        {
            Storage.MongoDB.SagaStorage.InitializeSagaDataTypes(ClientProvider.Client, databaseName, MongoPersistence.DefaultCollectionNamingConvention, SagaMetadataCollection);
            SagaStorage = new SagaPersister(SagaPersister.DefaultVersionElementName);
            
            SynchronizedStorage = new StorageSessionFactory(ClientProvider.Client, true, databaseName, MongoPersistence.DefaultCollectionNamingConvention, SessionTimeout ?? MongoPersistence.DefaultTransactionTimeout);

            var databaseSettings = new MongoDatabaseSettings
            {
                ReadConcern = ReadConcern.Majority,
                ReadPreference = ReadPreference.Primary,
                WriteConcern = WriteConcern.WMajority
            };
            var database = ClientProvider.Client.GetDatabase(databaseName, databaseSettings);
            await database.CreateCollectionAsync(MongoPersistence.DefaultCollectionNamingConvention(typeof(OutboxRecord)));
            OutboxStorage = new OutboxPersister(ClientProvider.Client, databaseName, MongoPersistence.DefaultCollectionNamingConvention);
        }

        public async Task Cleanup()
        {
            await ClientProvider.Client.DropDatabaseAsync(databaseName);
        }

        readonly string databaseName = "Test_" + DateTime.Now.Ticks.ToString(CultureInfo.InvariantCulture);
    }
}