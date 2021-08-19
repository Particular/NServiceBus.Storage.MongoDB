namespace NServiceBus.Storage.MongoDB.Tests
{
    using System;
    using System.Globalization;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;
    using Extensibility;
    using global::MongoDB.Driver;
    using MongoDB;
    using Persistence;
    using Sagas;

    public class SagaTestsConfiguration
    {
        public Func<ContextBag> GetContextBagForSagaStorage { get; set; } = () => new ContextBag();

        public SagaMetadataCollection SagaMetadataCollection
        {
            get
            {
                if (sagaMetadataCollection == null)
                {
                    var sagaTypes = Assembly.GetExecutingAssembly().GetTypes().Where(t => typeof(Saga).IsAssignableFrom(t) || typeof(IFindSagas<>).IsAssignableFrom(t) || typeof(IFinder).IsAssignableFrom(t)).ToArray();
                    sagaMetadataCollection = new SagaMetadataCollection();
                    sagaMetadataCollection.Initialize(sagaTypes);
                }
                return sagaMetadataCollection;
            }

            set
            {
                sagaMetadataCollection = value;
            }
        }

        SagaMetadataCollection sagaMetadataCollection;

        public SagaTestsConfiguration(string versionElementName, Func<Type, string> collectionNamingConvention, TimeSpan? transactionTimeout = null)
        {
            DatabaseName = "Test_" + DateTime.UtcNow.Ticks.ToString(CultureInfo.InvariantCulture);
            CollectionNamingConvention = collectionNamingConvention;

            SynchronizedStorage = new StorageSessionFactory(ClientProvider.Client, true, DatabaseName, collectionNamingConvention, transactionTimeout ?? MongoPersistence.DefaultTransactionTimeout);

            SagaStorage = new SagaPersister(versionElementName);
        }

        public SagaTestsConfiguration(TimeSpan? transactionTimeout = null) : this("_version", t => t.Name.ToLower(), transactionTimeout)
        {
        }

        public SagaTestsConfiguration(string versionElementName) : this(versionElementName, t => t.Name.ToLower())
        {
        }

        public string DatabaseName { get; }

        public Func<Type, string> CollectionNamingConvention { get; }

        public ISagaPersister SagaStorage { get; }

        public ISynchronizedStorage SynchronizedStorage { get; }

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

            MongoDB.SagaStorage.InitializeSagaDataTypes(ClientProvider.Client, DatabaseName, CollectionNamingConvention, SagaMetadataCollection);
        }

        public async Task Cleanup()
        {
            await ClientProvider.Client.DropDatabaseAsync(DatabaseName);
        }
    }
}