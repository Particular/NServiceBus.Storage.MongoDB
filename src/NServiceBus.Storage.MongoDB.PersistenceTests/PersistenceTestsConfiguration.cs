namespace NServiceBus.PersistenceTesting;

using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using NServiceBus.Outbox;
using NServiceBus.Sagas;
using Persistence;
using Storage.MongoDB;
using Storage.MongoDB.Tests;
using SynchronizedStorageSession = Storage.MongoDB.SynchronizedStorageSession;

public partial class PersistenceTestsConfiguration
{
    public bool SupportsDtc => false;

    public bool SupportsOutbox => true;

    public bool SupportsFinders => false;

    public bool SupportsPessimisticConcurrency => true;

    public ISagaIdGenerator SagaIdGenerator { get; } = new DefaultSagaIdGenerator();

    public Func<ICompletableSynchronizedStorageSession> CreateStorageSession { get; private set; }

    public ISagaPersister SagaStorage { get; private set; }

    public IOutboxStorage OutboxStorage { get; private set; }

    public async Task Configure(CancellationToken cancellationToken = default)
    {
        BsonSerializer.TryRegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));

        var memberMapCache = new MemberMapCache();
        Storage.MongoDB.SagaSchemaInstaller.InitializeSagaDataTypes(ClientProvider.Client, memberMapCache, databaseName,
            MongoPersistence.DefaultCollectionNamingConvention, SagaMetadataCollection);
        SagaStorage = new SagaPersister(SagaPersister.DefaultVersionElementName, memberMapCache);
        var synchronizedStorage = new StorageSessionFactory(ClientProvider.Client, true, databaseName,
            MongoPersistence.DefaultCollectionNamingConvention,
            SessionTimeout ?? MongoPersistence.DefaultTransactionTimeout);
        CreateStorageSession = () => new SynchronizedStorageSession(synchronizedStorage);

        var databaseSettings = new MongoDatabaseSettings
        {
            ReadConcern = ReadConcern.Majority,
            ReadPreference = ReadPreference.Primary,
            WriteConcern = WriteConcern.WMajority
        };
        var database = ClientProvider.Client.GetDatabase(databaseName, databaseSettings);
        await database.CreateCollectionAsync(MongoPersistence.DefaultCollectionNamingConvention(typeof(OutboxRecord)),
            cancellationToken: cancellationToken);
        OutboxStorage = new OutboxPersister(ClientProvider.Client, databaseName,
            MongoPersistence.DefaultCollectionNamingConvention);
    }

    public async Task Cleanup(CancellationToken cancellationToken = default) =>
        await ClientProvider.Client.DropDatabaseAsync(databaseName, cancellationToken);


    readonly string databaseName = $"Test_{DateTime.UtcNow.Ticks.ToString(CultureInfo.InvariantCulture)}";
}