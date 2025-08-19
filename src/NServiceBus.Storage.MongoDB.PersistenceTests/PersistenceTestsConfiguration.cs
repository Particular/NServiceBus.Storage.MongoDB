namespace NServiceBus.PersistenceTesting;

using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using NServiceBus.Outbox;
using NServiceBus.Sagas;
using Persistence;
using Storage.MongoDB;
using Storage.MongoDB.Tests;
using OutboxStorageFeature = Storage.MongoDB.OutboxStorage;
using SagaStorageFeature = Storage.MongoDB.SagaStorage;
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

        SagaStorageFeature.RegisterSagaEntityClassMappings(SagaMetadataCollection, memberMapCache);
        await SagaInstaller.CreateInfrastructureForSagaDataTypes(ClientProvider.Client, MongoPersistence.DefaultDatabaseSettings, memberMapCache, databaseName,
            MongoPersistence.DefaultCollectionNamingConvention, MongoPersistence.DefaultCollectionSettings, SagaMetadataCollection, cancellationToken);

        SagaStorage = new SagaPersister(SagaPersister.DefaultVersionElementName, memberMapCache);
        var synchronizedStorage = new StorageSessionFactory(ClientProvider.Client, true, databaseName,
            MongoPersistence.DefaultCollectionNamingConvention,
            SessionTimeout ?? MongoPersistence.DefaultTransactionTimeout);
        CreateStorageSession = () => new SynchronizedStorageSession(synchronizedStorage);

        OutboxStorageFeature.RegisterOutboxClassMappings();
        await OutboxInstaller.CreateInfrastructureForOutboxTypes(ClientProvider.Client, databaseName, MongoPersistence.DefaultDatabaseSettings, MongoPersistence.DefaultCollectionNamingConvention, MongoPersistence.DefaultCollectionSettings, TimeSpan.FromDays(7), cancellationToken);

        OutboxStorage = new OutboxPersister(ClientProvider.Client, databaseName, MongoPersistence.DefaultDatabaseSettings, MongoPersistence.DefaultCollectionNamingConvention, MongoPersistence.DefaultCollectionSettings);
    }

    public async Task Cleanup(CancellationToken cancellationToken = default) =>
        await ClientProvider.Client.DropDatabaseAsync(databaseName, cancellationToken);


    readonly string databaseName = $"Test_{DateTime.UtcNow.Ticks.ToString(CultureInfo.InvariantCulture)}";
}