namespace NServiceBus.Storage.MongoDB.Tests
{
    using System;
    using System.Globalization;
    using System.Threading.Tasks;
    using Extensibility;
    using global::MongoDB.Driver;
    using MongoDB;
    using NServiceBus.Outbox;
    using Persistence;

    public class OutboxTestsConfiguration
    {
        public Func<ContextBag> GetContextBagForOutboxStorage { get; set; } = () => new ContextBag();

        public OutboxTestsConfiguration(Func<Type, string> collectionNamingConvention, TimeSpan? transactionTimeout = null)
        {
            DatabaseName = "Test_" + DateTime.Now.Ticks.ToString(CultureInfo.InvariantCulture);
            CollectionNamingConvention = collectionNamingConvention;

            SynchronizedStorage = new StorageSessionFactory(ClientProvider.Client, true, DatabaseName, collectionNamingConvention, transactionTimeout ?? MongoPersistence.DefaultTransactionTimeout);
            transactionFactory = new MongoOutboxTransactionFactory(ClientProvider.Client, DatabaseName, CollectionNamingConvention, transactionTimeout ?? MongoPersistence.DefaultTransactionTimeout);
        }

        public OutboxTestsConfiguration(TimeSpan? transactionTimeout = null) : this(t => t.Name.ToLower(), transactionTimeout)
        {
        }

        public string DatabaseName { get; }

        public Func<Type, string> CollectionNamingConvention { get; }

        public IOutboxStorage OutboxStorage { get; private set; }

        public ISynchronizedStorage SynchronizedStorage { get; }

        public Task<OutboxTransaction> CreateTransaction(ContextBag context)
        {
            return transactionFactory.BeginTransaction(context);
        }

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

            MongoDB.OutboxStorage.InitializeOutboxTypes(ClientProvider.Client, DatabaseName, CollectionNamingConvention, TimeSpan.FromHours(1));

            OutboxStorage = new OutboxPersister(ClientProvider.Client, DatabaseName, CollectionNamingConvention);
        }

        public async Task Cleanup()
        {
            await ClientProvider.Client.DropDatabaseAsync(DatabaseName);
        }

        readonly MongoOutboxTransactionFactory transactionFactory;
    }
}