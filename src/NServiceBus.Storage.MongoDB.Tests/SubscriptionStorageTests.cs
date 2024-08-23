namespace NServiceBus.Storage.MongoDB.Tests
{
    using System;
    using System.Globalization;
    using System.Linq;
    using System.Threading.Tasks;
    using Extensibility;
    using NUnit.Framework;
    using MongoDB;
    using Unicast.Subscriptions;
    using Unicast.Subscriptions.MessageDrivenSubscriptions;

    [TestFixture]
    class SubscriptionStorageTests
    {
        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            DatabaseName = "Test_" + DateTime.UtcNow.Ticks.ToString(CultureInfo.InvariantCulture);

            var subscriptionCollection = ClientProvider.Client.GetDatabase(DatabaseName, MongoPersistence.DefaultDatabaseSettings).GetCollection<EventSubscription>("eventsubscriptions");
            var subscriptionPersister = new SubscriptionPersister(subscriptionCollection);
            subscriptionPersister.CreateIndexes();
            storage = subscriptionPersister;
        }

        [OneTimeTearDown]
        public async Task OneTimeTearDown()
        {
            await ClientProvider.Client.DropDatabaseAsync(DatabaseName);
        }

        [Test]
        public async Task Should_not_have_duplicate_subscriptions()
        {
            var eventType = CreateUniqueMessageType();

            await storage.Subscribe(new Subscriber("address1", "endpoint1"), eventType, new ContextBag());
            await storage.Subscribe(new Subscriber("address1", "endpoint1"), eventType, new ContextBag());

            var subscribers = await storage.GetSubscriberAddressesForMessage(new[]
            {
                eventType
            }, new ContextBag());

            Assert.That(subscribers.Count(), Is.EqualTo(1));
            var subscription = subscribers.Single();
            Assert.Multiple(() =>
            {
                Assert.That(subscription.Endpoint, Is.EqualTo("endpoint1"));
                Assert.That(subscription.TransportAddress, Is.EqualTo("address1"));
            });
        }

        [Test]
        public async Task Should_find_all_transport_addresses_of_logical_endpoint()
        {
            var eventType = CreateUniqueMessageType();

            await storage.Subscribe(new Subscriber("address1", "endpoint1"), eventType, new ContextBag());
            await storage.Subscribe(new Subscriber("address2", "endpoint1"), eventType, new ContextBag());

            var subscribers = await storage.GetSubscriberAddressesForMessage(new[]
            {
                eventType
            }, new ContextBag());

            Assert.That(subscribers.Count(), Is.EqualTo(2));
            Assert.That(subscribers.Select(s => s.TransportAddress), Is.EquivalentTo(new[] { "address1", "address2" }));
        }

        [Test]
        public async Task Should_update_endpoint_name_for_transport_address()
        {
            var eventType = CreateUniqueMessageType();

            await storage.Subscribe(new Subscriber("address1", "endpointA"), eventType, new ContextBag());
            await storage.Subscribe(new Subscriber("address1", "endpointB"), eventType, new ContextBag());

            var subscribers = await storage.GetSubscriberAddressesForMessage(new[]
            {
                eventType
            }, new ContextBag());

            Assert.Multiple(() =>
            {
                Assert.That(subscribers.Count(), Is.EqualTo(1));
                Assert.That(subscribers.Single().Endpoint, Is.EqualTo("endpointB"));
            });
        }

        [Test]
        public async Task Should_find_all_queried_message_types()
        {
            var eventType1 = CreateUniqueMessageType();
            var eventType2 = CreateUniqueMessageType();

            await storage.Subscribe(new Subscriber("address", "endpoint1"), eventType1, new ContextBag());
            await storage.Subscribe(new Subscriber("address", "endpoint1"), eventType2, new ContextBag());

            var subscribers = await storage.GetSubscriberAddressesForMessage(new[]
            {
                eventType1, eventType2
            }, new ContextBag());

            Assert.That(subscribers.Count(), Is.EqualTo(2));
            Assert.That(subscribers.Select(s => s.TransportAddress), Is.EquivalentTo(new[] { "address", "address" }));
        }

        [Test]
        public async Task Should_not_unsubscribe_when_address_does_not_match()
        {
            var eventType1 = CreateUniqueMessageType();

            await storage.Subscribe(new Subscriber("address", "endpoint1"), eventType1, new ContextBag());
            await storage.Unsubscribe(new Subscriber("another address", "endpoint1"), eventType1, new ContextBag());

            var subscribers = await storage.GetSubscriberAddressesForMessage(new[]
            {
                eventType1
            }, new ContextBag());

            Assert.That(subscribers.Single().TransportAddress, Is.EqualTo("address"));
        }

        [Test]
        public async Task Should_unsubscribe_when_logical_endpoint_does_not_match()
        {
            var eventType1 = CreateUniqueMessageType();

            await storage.Subscribe(new Subscriber("address", "endpoint1"), eventType1, new ContextBag());
            await storage.Subscribe(new Subscriber("address", "endpoint2"), eventType1, new ContextBag());
            await storage.Unsubscribe(new Subscriber("address", "endpoint1"), eventType1, new ContextBag());

            var subscribers = await storage.GetSubscriberAddressesForMessage(new[]
            {
                eventType1
            }, new ContextBag());

            Assert.That(subscribers.Count(), Is.EqualTo(0));
        }

        [Test]
        public async Task Should_handle_legacy_subscription_message()
        {
            var eventType = CreateUniqueMessageType();

            await storage.Subscribe(new Subscriber("address", null), eventType, new ContextBag());

            var subscribers = await storage.GetSubscriberAddressesForMessage(new[]
            {
                eventType
            }, new ContextBag());

            Assert.That(subscribers.Count(), Is.EqualTo(1));
            var subscriber = subscribers.Single();
            Assert.Multiple(() =>
            {
                Assert.That(subscriber.TransportAddress, Is.EqualTo("address"));
                Assert.That(subscriber.Endpoint, Is.Null);
            });
        }

        [Test]
        public async Task Should_add_endpoint_on_new_subscription()
        {
            var eventType = CreateUniqueMessageType();

            await storage.Subscribe(new Subscriber("address", null), eventType, new ContextBag());
            await storage.Subscribe(new Subscriber("address", "endpoint"), eventType, new ContextBag());

            var subscribers = await storage.GetSubscriberAddressesForMessage(new[]
            {
                eventType
            }, new ContextBag());

            Assert.That(subscribers.Count(), Is.EqualTo(1));
            var subscriber = subscribers.Single();
            Assert.Multiple(() =>
            {
                Assert.That(subscriber.TransportAddress, Is.EqualTo("address"));
                Assert.That(subscriber.Endpoint, Is.EqualTo("endpoint"));
            });
        }

        [Test]
        public async Task Should_not_remove_endpoint_on_legacy_subscriptions()
        {
            var eventType = CreateUniqueMessageType();

            await storage.Subscribe(new Subscriber("address", "endpoint"), eventType, new ContextBag());
            await storage.Subscribe(new Subscriber("address", null), eventType, new ContextBag());

            var subscribers = await storage.GetSubscriberAddressesForMessage(new[]
            {
                eventType
            }, new ContextBag());

            Assert.That(subscribers.Count(), Is.EqualTo(1));
            var subscriber = subscribers.Single();
            Assert.Multiple(() =>
            {
                Assert.That(subscriber.TransportAddress, Is.EqualTo("address"));
                Assert.That(subscriber.Endpoint, Is.EqualTo("endpoint"));
            });
        }

        [Test]
        public async Task Should_ignore_message_version_on_subscriptions()
        {
            await storage.Subscribe(new Subscriber("subscriberA@server1", "subscriberA"), new MessageType("SomeMessage", "1.0.0"), new ContextBag());

            var subscribers = await storage.GetSubscriberAddressesForMessage(new[]
            {
                new MessageType("SomeMessage", "2.0.0")
            }, new ContextBag());

            Assert.That(subscribers.Single().Endpoint, Is.EqualTo("subscriberA"));
        }

        static MessageType CreateUniqueMessageType()
        {
            return new MessageType(Guid.NewGuid().ToString("N"), "1.0.0");
        }

        ISubscriptionStorage storage;
        string DatabaseName;
    }
}