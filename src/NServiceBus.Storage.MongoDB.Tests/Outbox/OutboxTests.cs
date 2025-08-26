namespace NServiceBus.Storage.MongoDB.Tests;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NServiceBus.Outbox;
using NUnit.Framework;

public class OutboxStorageTest : OutboxPersisterTests
{
    [Test]
    public async Task Should_return_same_data()
    {
        var msgId = RandomString();

        var operations = new TransportOperation[]
        {
            new(RandomString(), FillDictionary(new Transport.DispatchProperties(), 3),
                Encoding.UTF8.GetBytes(RandomString()), FillDictionary(new Dictionary<string, string>(), 3)),
            new(RandomString(), FillDictionary(new Transport.DispatchProperties(), 3),
                Encoding.UTF8.GetBytes(RandomString()), FillDictionary(new Dictionary<string, string>(), 3)),
            new(RandomString(), FillDictionary(new Transport.DispatchProperties(), 3),
                Encoding.UTF8.GetBytes(RandomString()), FillDictionary(new Dictionary<string, string>(), 3)),
        };
        var testMessage = new OutboxMessage(msgId, operations);

        var context = configuration.GetContextBagForOutboxStorage();
        var empty = await configuration.OutboxStorage.Get(msgId, context);
        Assert.That(empty, Is.Null);

        using (var transaction = await configuration.CreateTransaction(context))
        {
            await configuration.OutboxStorage.Store(testMessage, transaction, context);
            await transaction.Commit();
        }

        var received = await configuration.OutboxStorage.Get(msgId, context);

        AreSame(received, msgId, operations);
    }

    [Test]
    public async Task Should_support_legacy_format_in_get()
    {
        var msgId = RandomString();

        var database = ClientProvider.Client.GetDatabase(configuration.DatabaseName, MongoPersistence.DefaultDatabaseSettings);
        var outboxCollection = database.GetCollection<DuckTypeOutboxRecord>(configuration.CollectionNamingConvention(typeof(OutboxRecord)), MongoPersistence.DefaultCollectionSettings);

        var transportOperation = new TransportOperation(RandomString(), FillDictionary(new Transport.DispatchProperties(), 3),
            Encoding.UTF8.GetBytes(RandomString()), FillDictionary(new Dictionary<string, string>(), 3));

        var transportOperations = new[] { transportOperation };

        var storageOperations = transportOperations.Select(o => new StorageTransportOperation(o)).ToArray();

        await outboxCollection.InsertOneAsync(new DuckTypeOutboxRecord
        {
            Id = msgId,
            TransportOperations = storageOperations,
        });

        var context = configuration.GetContextBagForOutboxStorage();

        var received = await configuration.OutboxStorage.Get(msgId, context);

        AreSame(received, msgId, transportOperations);
    }

    static void AreSame(OutboxMessage received, string msgId, TransportOperation[] operations)
    {
        Assert.Multiple(() =>
        {
            Assert.That(received.MessageId, Is.EqualTo(msgId));
            Assert.That(received.TransportOperations, Has.Length.EqualTo(operations.Length));
        });

        for (var op = 0; op < operations.Length; op++)
        {
            var expectedOp = operations[op];
            var receivedOp = received.TransportOperations[op];

            Assert.Multiple(() =>
            {
                Assert.That(receivedOp.MessageId, Is.EqualTo(expectedOp.MessageId));
                Assert.That(Convert.ToBase64String(receivedOp.Body.ToArray()),
                    Is.EqualTo(Convert.ToBase64String(expectedOp.Body.ToArray())));
            });
            foreach (var header in expectedOp.Headers.Keys)
            {
                Assert.That(receivedOp.Headers[header], Is.EqualTo(expectedOp.Headers[header]));
            }

            foreach (var property in expectedOp.Options.Keys)
            {
                Assert.That(receivedOp.Options[property], Is.EqualTo(expectedOp.Options[property]));
            }
        }
    }

    [Test]
    public async Task Should_support_legacy_format_in_set_as_dispatched()
    {
        var msgId = RandomString();

        var database = ClientProvider.Client.GetDatabase(configuration.DatabaseName, MongoPersistence.DefaultDatabaseSettings);
        var outboxCollection = database.GetCollection<DuckTypeOutboxRecord>(configuration.CollectionNamingConvention(typeof(OutboxRecord)), MongoPersistence.DefaultCollectionSettings);

        var transportOperation = new TransportOperation(RandomString(), FillDictionary(new Transport.DispatchProperties(), 3),
            Encoding.UTF8.GetBytes(RandomString()), FillDictionary(new Dictionary<string, string>(), 3));

        var transportOperations = new[] { transportOperation };

        var storageOperations = transportOperations.Select(o => new StorageTransportOperation(o)).ToArray();

        await outboxCollection.InsertOneAsync(new DuckTypeOutboxRecord
        {
            Id = msgId,
            TransportOperations = storageOperations,
        });

        var context = configuration.GetContextBagForOutboxStorage();

        await configuration.OutboxStorage.SetAsDispatched(msgId, context);

        var receivedAfter = await configuration.OutboxStorage.Get(msgId, context);

        Assert.That(receivedAfter.TransportOperations, Is.Empty);
    }

    class DuckTypeOutboxRecord
    {
        public string Id { get; set; }
        public DateTime? Dispatched { get; set; }

        public StorageTransportOperation[] TransportOperations { get; set; }
    }

    string RandomString() => Guid.NewGuid().ToString();

    T FillDictionary<T>(T dictionary, int count)
        where T : Dictionary<string, string>
    {
        for (var i = 0; i < count; i++)
        {
            dictionary[i.ToString()] = RandomString();
        }

        return dictionary;
    }
}