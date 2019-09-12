namespace NServiceBus.Persistence.ComponentTests
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Extensibility;
    using NUnit.Framework;
    using Unicast.Subscriptions;
    using Unicast.Subscriptions.MessageDrivenSubscriptions;

    [TestFixture]
    class SubscriptionStorageTests
    {
        PersistenceTestsConfiguration configuration;

        [OneTimeSetUp]
        public async Task OneTimeSetUp()
        {
            configuration = new PersistenceTestsConfiguration();
            await configuration.Configure();
        }

        [OneTimeTearDown]
        public async Task OneTimeTearDown()
        {
            await configuration.Cleanup();
        }

        [Test]
        public async Task Should_not_have_duplicate_subscriptions()
        {
            configuration.RequiresSubscriptionSupport();
            var storage = configuration.SubscriptionStorage;

            var eventType = CreateUniqueMessageType();

            await storage.Subscribe(new Subscriber("address1", "endpoint1"), eventType, new ContextBag());
            await storage.Subscribe(new Subscriber("address1", "endpoint1"), eventType, new ContextBag());

            var subscribers = await storage.GetSubscriberAddressesForMessage(new[]
            {
                eventType
            }, configuration.GetContextBagForSubscriptions());

            Assert.AreEqual(1, subscribers.Count());
            var subscription = subscribers.Single();
            Assert.AreEqual("endpoint1", subscription.Endpoint);
            Assert.AreEqual("address1", subscription.TransportAddress);
        }

        [Test]
        public async Task Should_find_all_transport_addresses_of_logical_endpoint()
        {
            configuration.RequiresSubscriptionSupport();
            var storage = configuration.SubscriptionStorage;

            var eventType = CreateUniqueMessageType();

            await storage.Subscribe(new Subscriber("address1", "endpoint1"), eventType, new ContextBag());
            await storage.Subscribe(new Subscriber("address2", "endpoint1"), eventType, new ContextBag());

            var subscribers = await storage.GetSubscriberAddressesForMessage(new[]
            {
                eventType
            }, configuration.GetContextBagForSubscriptions());

            Assert.AreEqual(2, subscribers.Count());
            CollectionAssert.AreEquivalent(new[] { "address1", "address2" }, subscribers.Select(s => s.TransportAddress));
        }

        [Test]
        public async Task Should_find_all_logical_endpoints_of_same_transport_address()
        {
            configuration.RequiresSubscriptionSupport();
            var storage = configuration.SubscriptionStorage;

            var eventType = CreateUniqueMessageType();

            await storage.Subscribe(new Subscriber("address1", "endpointA"), eventType, new ContextBag());
            await storage.Subscribe(new Subscriber("address1", "endpointB"), eventType, new ContextBag());

            var subscribers = await storage.GetSubscriberAddressesForMessage(new[]
            {
                eventType
            }, configuration.GetContextBagForSubscriptions());

            Assert.AreEqual(2, subscribers.Count());
            CollectionAssert.AreEquivalent(new[] { "endpointA", "endpointB" }, subscribers.Select(s => s.Endpoint));
        }

        [Test]
        public async Task Should_find_all_queried_message_types()
        {
            configuration.RequiresSubscriptionSupport();
            var storage = configuration.SubscriptionStorage;

            var eventType1 = CreateUniqueMessageType();
            var eventType2 = CreateUniqueMessageType();

            await storage.Subscribe(new Subscriber("address", "endpoint1"), eventType1, new ContextBag());
            await storage.Subscribe(new Subscriber("address", "endpoint1"), eventType2, new ContextBag());

            var subscribers = await storage.GetSubscriberAddressesForMessage(new[]
            {
                eventType1, eventType2
            }, configuration.GetContextBagForSubscriptions());

            Assert.AreEqual(2, subscribers.Count());
            CollectionAssert.AreEquivalent(new[] { "address", "address" }, subscribers.Select(s => s.TransportAddress));
        }

        [Test]
        public async Task Should_not_unsubscribe_when_address_does_not_match()
        {
            configuration.RequiresSubscriptionSupport();
            var storage = configuration.SubscriptionStorage;

            var eventType1 = CreateUniqueMessageType();

            await storage.Subscribe(new Subscriber("address", "endpoint1"), eventType1, new ContextBag());
            await storage.Unsubscribe(new Subscriber("another address", "endpoint1"), eventType1, new ContextBag());

            var subscribers = await storage.GetSubscriberAddressesForMessage(new[]
            {
                eventType1
            }, configuration.GetContextBagForSubscriptions());

            Assert.AreEqual("address", subscribers.Single().TransportAddress);
        }

        [Test]
        public async Task Should_unsubscribe_when_logical_endpoint_does_not_match()
        {
            configuration.RequiresSubscriptionSupport();
            var storage = configuration.SubscriptionStorage;

            var eventType1 = CreateUniqueMessageType();

            await storage.Subscribe(new Subscriber("address", "endpoint1"), eventType1, new ContextBag());
            await storage.Subscribe(new Subscriber("address", "endpoint2"), eventType1, new ContextBag());
            await storage.Unsubscribe(new Subscriber("address", "endpoint1"), eventType1, new ContextBag());

            var subscribers = await storage.GetSubscriberAddressesForMessage(new[]
            {
                eventType1
            }, configuration.GetContextBagForSubscriptions());

            Assert.AreEqual(0, subscribers.Count());
        }

        [Test]
        public async Task Should_ignore_message_version_on_subscriptions()
        {
            configuration.RequiresSubscriptionSupport();
            var storage = configuration.SubscriptionStorage;

            await storage.Subscribe(new Subscriber("subscriberA@server1", "subscriberA"), new MessageType("SomeMessage", "1.0.0"), configuration.GetContextBagForSubscriptions());

            var subscribers = await storage.GetSubscriberAddressesForMessage(new[]
            {
                new MessageType("SomeMessage", "2.0.0")
            }, configuration.GetContextBagForSubscriptions());

            Assert.AreEqual("subscriberA", subscribers.Single().Endpoint);
        }

        static MessageType CreateUniqueMessageType()
        {
            return new MessageType(Guid.NewGuid().ToString("N"), "1.0.0");
        }
    }
}