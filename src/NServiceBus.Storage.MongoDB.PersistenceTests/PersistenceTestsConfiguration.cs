namespace NServiceBus.PersistenceTesting
{
    using System;
    using System.Globalization;
    using System.Threading;
    using System.Threading.Tasks;
    using MongoDB.Driver;
    using NServiceBus.Outbox;
    using NServiceBus.Sagas;
    using Persistence;
    using Storage.MongoDB;

    public partial class PersistenceTestsConfiguration
    {
        public bool SupportsDtc => false;

        public bool SupportsOutbox => true;

        public bool SupportsFinders => false;

        public bool SupportsPessimisticConcurrency => true;

        public ISagaIdGenerator SagaIdGenerator { get; } = new DefaultSagaIdGenerator();

        public ISagaPersister SagaStorage { get; private set; }

        public ISynchronizedStorage SynchronizedStorage { get; private set; }

        public ISynchronizedStorageAdapter SynchronizedStorageAdapter { get; private set; }

        public IOutboxStorage OutboxStorage { get; private set; }

        public async Task Configure(CancellationToken cancellationToken = default)
        {
            var containerConnectionString = Environment.GetEnvironmentVariable("NServiceBusStorageMongoDB_ConnectionString");
            client = string.IsNullOrWhiteSpace(containerConnectionString) ? new MongoClient() : new MongoClient(containerConnectionString);

            Storage.MongoDB.SagaStorage.InitializeSagaDataTypes(client, databaseName, MongoPersistence.DefaultCollectionNamingConvention, SagaMetadataCollection);
            SagaStorage = new SagaPersister(SagaPersister.DefaultVersionElementName);
            SynchronizedStorage = new StorageSessionFactory(client, true, databaseName, MongoPersistence.DefaultCollectionNamingConvention, SessionTimeout ?? MongoPersistence.DefaultTransactionTimeout);

            var databaseSettings = new MongoDatabaseSettings
            {
                ReadConcern = ReadConcern.Majority,
                ReadPreference = ReadPreference.Primary,
                WriteConcern = WriteConcern.WMajority
            };
            var database = client.GetDatabase(databaseName, databaseSettings);
            await database.CreateCollectionAsync(MongoPersistence.DefaultCollectionNamingConvention(typeof(OutboxRecord)), cancellationToken: cancellationToken);
            OutboxStorage = new OutboxPersister(client, databaseName, MongoPersistence.DefaultCollectionNamingConvention);
        }

        public async Task Cleanup(CancellationToken cancellationToken = default)
        {
            await client.DropDatabaseAsync(databaseName, cancellationToken);
        }

        readonly string databaseName = "Test_" + DateTime.UtcNow.Ticks.ToString(CultureInfo.InvariantCulture);
        MongoClient client;
    }
}