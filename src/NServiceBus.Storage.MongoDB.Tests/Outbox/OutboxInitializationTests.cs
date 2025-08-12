namespace NServiceBus.Storage.MongoDB.Tests;

using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using global::MongoDB.Bson;
using global::MongoDB.Driver;
using NUnit.Framework;

[TestFixture]
public class OutboxInitializationTests
{
    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        databaseName = "Test_" + DateTime.UtcNow.Ticks.ToString(CultureInfo.InvariantCulture);

        var database = ClientProvider.Client.GetDatabase(databaseName, MongoPersistence.DefaultDatabaseSettings);

        await database.CreateCollectionAsync(CollectionNamingConvention<OutboxRecord>());

        outboxCollection = ClientProvider.Client.GetDatabase(databaseName, MongoPersistence.DefaultDatabaseSettings)
            .GetCollection<OutboxRecord>(CollectionNamingConvention<OutboxRecord>(), MongoPersistence.DefaultCollectionSettings);
    }

    static string CollectionNamingConvention<T>() => CollectionNamingConvention(typeof(T));

    static string CollectionNamingConvention(Type type) => type.Name.ToLower();

    [SetUp]
    public async Task Setup() => await outboxCollection.Indexes.DropAllAsync();

    [Theory]
    public async Task Should_create_index_when_it_doesnt_exist(TimeSpan timeToKeepOutboxDeduplicationData)
    {
        OutboxSchemaInstaller.InitializeOutboxTypes(ClientProvider.Client, databaseName, MongoPersistence.DefaultDatabaseSettings, CollectionNamingConvention,
            MongoPersistence.DefaultCollectionSettings, timeToKeepOutboxDeduplicationData);

        await AssertIndexCorrect(outboxCollection, timeToKeepOutboxDeduplicationData);
    }

    [Theory]
    public async Task Should_recreate_when_expiry_drifts(TimeSpan timeToKeepOutboxDeduplicationData)
    {
        OutboxSchemaInstaller.InitializeOutboxTypes(ClientProvider.Client, databaseName, MongoPersistence.DefaultDatabaseSettings, CollectionNamingConvention,
            MongoPersistence.DefaultCollectionSettings, timeToKeepOutboxDeduplicationData.Add(TimeSpan.FromSeconds(30)));

        OutboxSchemaInstaller.InitializeOutboxTypes(ClientProvider.Client, databaseName, MongoPersistence.DefaultDatabaseSettings, CollectionNamingConvention,
            MongoPersistence.DefaultCollectionSettings, timeToKeepOutboxDeduplicationData);

        await AssertIndexCorrect(outboxCollection, timeToKeepOutboxDeduplicationData);
    }

    [Theory]
    public async Task Should_recreate_when_expiry_column_dropped(TimeSpan timeToKeepOutboxDeduplicationData)
    {
        var indexModel = new CreateIndexModel<OutboxRecord>(
            Builders<OutboxRecord>.IndexKeys.Ascending(record => record.Dispatched),
            new CreateIndexOptions { Name = OutboxSchemaInstaller.OutboxCleanupIndexName, Background = true });
        await outboxCollection.Indexes.CreateOneAsync(indexModel);

        OutboxSchemaInstaller.InitializeOutboxTypes(ClientProvider.Client, databaseName, MongoPersistence.DefaultDatabaseSettings, CollectionNamingConvention,
            MongoPersistence.DefaultCollectionSettings, timeToKeepOutboxDeduplicationData);

        await AssertIndexCorrect(outboxCollection, timeToKeepOutboxDeduplicationData);
    }

    [DatapointSource]
    public TimeSpan[] Expiry = [TimeSpan.FromHours(1), TimeSpan.FromHours(3), TimeSpan.FromDays(1)];

    static async Task AssertIndexCorrect(IMongoCollection<OutboxRecord> outboxCollection, TimeSpan expiry)
    {
        var outboxCleanupIndex = (await outboxCollection.Indexes.ListAsync()).ToList().SingleOrDefault(indexDocument =>
            indexDocument.GetElement("name").Value == OutboxSchemaInstaller.OutboxCleanupIndexName);

        Assert.That(outboxCleanupIndex, Is.Not.Null);

        BsonElement bsonElement = outboxCleanupIndex.GetElement("expireAfterSeconds");
        Assert.That(TimeSpan.FromSeconds(bsonElement.Value.ToInt32()), Is.EqualTo(expiry));
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown() => await ClientProvider.Client.DropDatabaseAsync(databaseName);

    IMongoCollection<OutboxRecord> outboxCollection;
    string databaseName;
}