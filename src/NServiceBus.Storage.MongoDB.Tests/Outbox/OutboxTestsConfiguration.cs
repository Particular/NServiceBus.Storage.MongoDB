namespace NServiceBus.Storage.MongoDB.Tests;

using System;
using System.Globalization;
using System.Threading.Tasks;
using Extensibility;
using Outbox;

public class OutboxTestsConfiguration
{
    public Func<ContextBag> GetContextBagForOutboxStorage { get; set; } = () => new ContextBag();

    public OutboxTestsConfiguration(Func<Type, string> collectionNamingConvention, TimeSpan? transactionTimeout = null)
    {
        DatabaseName = "Test_" + DateTime.UtcNow.Ticks.ToString(CultureInfo.InvariantCulture);
        CollectionNamingConvention = collectionNamingConvention;

        transactionFactory = new MongoOutboxTransactionFactory(ClientProvider.Client, DatabaseName,
            CollectionNamingConvention, transactionTimeout ?? MongoPersistence.DefaultTransactionTimeout);
    }

    public OutboxTestsConfiguration(TimeSpan? transactionTimeout = null) : this(t => t.Name.ToLower(),
        transactionTimeout)
    {
    }

    public string DatabaseName { get; }

    public Func<Type, string> CollectionNamingConvention { get; }

    public IOutboxStorage OutboxStorage { get; private set; }

    public Task<IOutboxTransaction> CreateTransaction(ContextBag context) => transactionFactory.BeginTransaction(context);

    public async Task Configure()
    {
        var database = ClientProvider.Client.GetDatabase(DatabaseName, MongoPersistence.DefaultDatabaseSettings);

        await database.CreateCollectionAsync(CollectionNamingConvention(typeof(OutboxRecord)));

        await OutboxInstaller.CreateInfrastructureForOutboxTypes(ClientProvider.Client, DatabaseName, MongoPersistence.DefaultDatabaseSettings, CollectionNamingConvention,
            MongoPersistence.DefaultCollectionSettings, TimeSpan.FromHours(1));

        OutboxStorage = new OutboxPersister(ClientProvider.Client, DatabaseName, CollectionNamingConvention);
    }

    public async Task Cleanup() => await ClientProvider.Client.DropDatabaseAsync(DatabaseName);

    readonly MongoOutboxTransactionFactory transactionFactory;
}