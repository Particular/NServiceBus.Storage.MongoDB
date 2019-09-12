namespace NServiceBus.Storage.MongoDB.Subscriptions
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Extensibility;
    using global::MongoDB.Bson;
    using global::MongoDB.Driver;
    using Unicast.Subscriptions;
    using Unicast.Subscriptions.MessageDrivenSubscriptions;

    class SubscriptionPersister : ISubscriptionStorage
    {
        IMongoCollection<EventSubscription> subscriptionsCollection;

        public SubscriptionPersister(IMongoCollection<EventSubscription> subscriptionsCollection)
        {
            this.subscriptionsCollection = subscriptionsCollection;
        }

        public async Task Subscribe(Subscriber subscriber, MessageType messageType, ContextBag context)
        {
            var subscription = new EventSubscription();
            subscription.MessageTypeName = messageType.TypeName;
            subscription.TransportAddress = subscriber.TransportAddress;
            subscription.Endpoint = subscriber.Endpoint;

            //TODO catch exception?
            //TODO use update instead of insert with upsert:true
            try
            {
                await subscriptionsCollection.InsertOneAsync(subscription).ConfigureAwait(false);
            }
            catch (MongoWriteException e) when(e.WriteError?.Code == 11000)
            {
                // duplicate key error
            }
            
        }

        public async Task Unsubscribe(Subscriber subscriber, MessageType messageType, ContextBag context)
        {
            var filter = Builders<EventSubscription>.Filter.And(
                Builders<EventSubscription>.Filter.Eq(s => s.MessageTypeName, messageType.TypeName),
                Builders<EventSubscription>.Filter.Eq(s => s.TransportAddress, subscriber.TransportAddress));
            await subscriptionsCollection.DeleteOneAsync(filter).ConfigureAwait(false);
        }

        public async Task<IEnumerable<Subscriber>> GetSubscriberAddressesForMessage(IEnumerable<MessageType> messageTypes, ContextBag context)
        {
            var messageTypeNames = messageTypes.Select(t => t.TypeName).ToArray();
            var filter = Builders<EventSubscription>.Filter.In(s => s.MessageTypeName, messageTypeNames);

            var options = new FindOptions();
            var result = await subscriptionsCollection.Find(filter, options).ToListAsync().ConfigureAwait(false);

            return result.Select(r => new Subscriber(r.TransportAddress, r.Endpoint));
        }

        public void CreateIndexes()
        {
            var indexKeyDefintion = Builders<EventSubscription>.IndexKeys
                .Ascending(x => x.MessageTypeName)
                .Ascending(x => x.TransportAddress)
                .Ascending(x => x.Endpoint); // allow queries to return results directly from the index
            var index = new CreateIndexModel<EventSubscription>(indexKeyDefintion, new CreateIndexOptions { Unique = true });
            subscriptionsCollection.Indexes.CreateOne(index);
        }
    }

    //TODO: should we add a timestamp for debug purpose?
    class EventSubscription
    {
        public ObjectId Id { get; set; }
        public string MessageTypeName { get; set; }
        public string TransportAddress { get; set; }
        public string Endpoint { get; set; }
    }
}