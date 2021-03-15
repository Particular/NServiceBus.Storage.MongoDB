﻿namespace NServiceBus.Storage.MongoDB.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading.Tasks;
    using NServiceBus.Extensibility;
    using NUnit.Framework;

    public class OutboxStorageTest : OutboxPersisterTests
    {
        [Test]
        public async Task Should_return_same_data()
        {
            var msgId = RandomString();

            var operations = new Outbox.TransportOperation[]
            {
                new Outbox.TransportOperation(RandomString(), FillDictionary(new Transport.DispatchProperties(), 3), Encoding.UTF8.GetBytes(RandomString()), FillDictionary(new Dictionary<string, string>(), 3)),
                new Outbox.TransportOperation(RandomString(), FillDictionary(new Transport.DispatchProperties(), 3), Encoding.UTF8.GetBytes(RandomString()), FillDictionary(new Dictionary<string, string>(), 3)),
                new Outbox.TransportOperation(RandomString(), FillDictionary(new Transport.DispatchProperties(), 3), Encoding.UTF8.GetBytes(RandomString()), FillDictionary(new Dictionary<string, string>(), 3)),

            };
            var testMessage = new Outbox.OutboxMessage(msgId, operations);

            var context = new ContextBag();
            var empty = await configuration.OutboxStorage.Get(msgId, context);
            Assert.IsNull(empty);

            using (var transaction = await configuration.CreateTransaction(context))
            {
                await configuration.OutboxStorage.Store(testMessage, transaction, context);
                await transaction.Commit();
            }

            var received = await configuration.OutboxStorage.Get(msgId, context);

            Assert.AreEqual(msgId, received.MessageId);
            Assert.AreEqual(operations.Length, received.TransportOperations.Length);

            for (var op = 0; op < operations.Length; op++)
            {
                var expectedOp = operations[op];
                var receivedOp = received.TransportOperations[op];

                Assert.AreEqual(expectedOp.MessageId, receivedOp.MessageId);
                Assert.AreEqual(Convert.ToBase64String(expectedOp.Body), Convert.ToBase64String(receivedOp.Body));
                foreach (var header in expectedOp.Headers.Keys)
                {
                    Assert.AreEqual(expectedOp.Headers[header], receivedOp.Headers[header]);
                }
                foreach (var property in expectedOp.Options.Keys)
                {
                    Assert.AreEqual(expectedOp.Options[property], receivedOp.Options[property]);
                }
            }
        }

        string RandomString()
        {
            return Guid.NewGuid().ToString();
        }

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
}
