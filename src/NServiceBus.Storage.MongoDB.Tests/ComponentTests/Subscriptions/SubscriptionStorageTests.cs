namespace NServiceBus.Persistence.ComponentTests
{
    using System.Linq;
    using System.Threading.Tasks;
    using ComponentTests;
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

            await storage.Subscribe(new Subscriber("address1", "endpoint1"), new MessageType("message1", "1.0.0"), new ContextBag());
            await storage.Subscribe(new Subscriber("address1", "endpoint1"), new MessageType("message1", "2.0.0"), new ContextBag());

            var subscribers = await storage.GetSubscriberAddressesForMessage(new[]
            {
                new MessageType("message1", "1.0.0")
            }, configuration.GetContextBagForSubscriptions());

            Assert.AreEqual(1, subscribers.Count());
            var subscription = subscribers.Single();
            Assert.AreEqual("endpoint1", subscription.Endpoint);
            Assert.AreEqual("address1", subscription.TransportAddress);
        }

        [Test]
        public async Task Should_return_all_transport_addresses_of_logical_endpoint()
        {
            configuration.RequiresSubscriptionSupport();
            var storage = configuration.SubscriptionStorage;

            await storage.Subscribe(new Subscriber("address1", "endpoint1"), new MessageType("message2", "1.0.0"), new ContextBag());
            await storage.Subscribe(new Subscriber("address2", "endpoint1"), new MessageType("message2", "1.0.0"), new ContextBag());

            var subscribers = await storage.GetSubscriberAddressesForMessage(new[]
            {
                new MessageType("message2", "1.0.0")
            }, configuration.GetContextBagForSubscriptions());

            Assert.AreEqual(2, subscribers.Count());
            CollectionAssert.AreEquivalent(new[] { "address1", "address2" }, subscribers.Select(s => s.TransportAddress));
        }

        [Test]
        public async Task Should_return_all_queried_message_types()
        {
            configuration.RequiresSubscriptionSupport();
            var storage = configuration.SubscriptionStorage;

            await storage.Subscribe(new Subscriber("address", "endpoint1"), new MessageType("message3", "1.0.0"), new ContextBag());
            await storage.Subscribe(new Subscriber("address", "endpoint1"), new MessageType("message4", "1.0.0"), new ContextBag());

            var subscribers = await storage.GetSubscriberAddressesForMessage(new[]
            {
                new MessageType("message3", "1.0.0"), new MessageType("message4", "1.0.0")
            }, configuration.GetContextBagForSubscriptions());

            Assert.AreEqual(2, subscribers.Count());
            CollectionAssert.AreEquivalent(new[] { "address", "address" }, subscribers.Select(s => s.TransportAddress));
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
    }
}