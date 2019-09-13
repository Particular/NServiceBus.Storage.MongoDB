namespace NServiceBus.Storage.MongoDB.Subscriptions
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Extensibility;
    using global::MongoDB.Driver;
    using Logging;
    using Unicast.Subscriptions;
    using Unicast.Subscriptions.MessageDrivenSubscriptions;

    class SubscriptionPersister : ISubscriptionStorage
    {
        public SubscriptionPersister(IMongoCollection<EventSubscription> subscriptionsCollection)
        {
            this.subscriptionsCollection = subscriptionsCollection;
        }

        public async Task Subscribe(Subscriber subscriber, MessageType messageType, ContextBag context)
        {
            var subscription = new EventSubscription
            {
                MessageTypeName = messageType.TypeName,
                TransportAddress = subscriber.TransportAddress,
                Endpoint = subscriber.Endpoint
            };

            if (subscriber.Endpoint != null)
            {
                var filter = Builders<EventSubscription>.Filter.And(
                    Builders<EventSubscription>.Filter.Eq(s => s.MessageTypeName, messageType.TypeName),
                    Builders<EventSubscription>.Filter.Eq(s => s.TransportAddress, subscriber.TransportAddress));
                var update = Builders<EventSubscription>.Update.Set(s => s.Endpoint, subscriber.Endpoint);
                var options = new UpdateOptions {IsUpsert = true};

                var result = await subscriptionsCollection.UpdateOneAsync(filter, update, options).ConfigureAwait(false);
                if (result.ModifiedCount > 0)
                {
                    Log.Debug($"Updated existing subscription of '{subscriber.TransportAddress}' on '{messageType.TypeName}'");
                } else if (result.UpsertedId != null)
                {
                    Log.Debug($"Created new subscription for {subscriber.TransportAddress} on '{messageType.TypeName}'");
                }
            }
            else
            {
                // support for older versions of NServiceBus which do not provide a logical endpoint name
                try
                {
                    await subscriptionsCollection.InsertOneAsync(subscription).ConfigureAwait(false);
                    Log.Debug($"Created legacy subscription for '{subscriber.TransportAddress}' on '{messageType.TypeName}'");
                }
                catch (MongoWriteException e) when (e.WriteError?.Code == DuplicateKeyErrorCode)
                {
                    // duplicate key error which means a document already exists
                    // existing subscriptions should not be stripped of their logical endpoint name
                    Log.Debug($"Skipping legacy subscription for '{subscriber.TransportAddress}' on '{messageType.TypeName}' because a newer subscription already exists");
                }
            }
        }

        public async Task Unsubscribe(Subscriber subscriber, MessageType messageType, ContextBag context)
        {
            var filter = Builders<EventSubscription>.Filter.And(
                Builders<EventSubscription>.Filter.Eq(s => s.MessageTypeName, messageType.TypeName),
                Builders<EventSubscription>.Filter.Eq(s => s.TransportAddress, subscriber.TransportAddress));
            var result = await subscriptionsCollection.DeleteManyAsync(filter).ConfigureAwait(false);

            Log.Debug($"Deleted {result.DeletedCount} subscriptions for address '{subscriber.TransportAddress}' on message type '{messageType.TypeName}'");
        }

        public async Task<IEnumerable<Subscriber>> GetSubscriberAddressesForMessage(IEnumerable<MessageType> messageTypes, ContextBag context)
        {
            var messageTypeNames = messageTypes.Select(t => t.TypeName).ToArray();
            var filter = Builders<EventSubscription>.Filter.In(s => s.MessageTypeName, messageTypeNames);
            // This projection allows a covered query:
            var projection = Builders<EventSubscription>.Projection
                .Include(s => s.TransportAddress)
                .Include(s => s.Endpoint)
                .Exclude("_id");

            //var options = new FindOptions();
            //options.Modifiers = new BsonDocument("$explain", true);
            //var queryStats = await subscriptionsCollection.Find(filter, options).Project(p).ToListAsync().ConfigureAwait(false);

            var result = await subscriptionsCollection.Find(filter).Project(projection).ToListAsync().ConfigureAwait(false);

            return result.Select(r => new Subscriber(
                r[nameof(EventSubscription.TransportAddress)].AsString,
                r[nameof(EventSubscription.Endpoint)].IsBsonNull ? null : r[nameof(EventSubscription.Endpoint)].AsString));
        }

        public void CreateIndexes()
        {
            var uniqueIndex = new CreateIndexModel<EventSubscription>(Builders<EventSubscription>.IndexKeys
                    .Ascending(x => x.MessageTypeName)
                    .Ascending(x => x.TransportAddress),
                new CreateIndexOptions
                    {Unique = true});
            var searchIndex = new CreateIndexModel<EventSubscription>(Builders<EventSubscription>.IndexKeys
                .Ascending(x => x.MessageTypeName)
                .Ascending(x => x.TransportAddress)
                .Ascending(x => x.Endpoint));
            subscriptionsCollection.Indexes.CreateMany(new[] {uniqueIndex, searchIndex});
        }

        IMongoCollection<EventSubscription> subscriptionsCollection;
        static readonly ILog Log = LogManager.GetLogger<SubscriptionPersister>();
        const int DuplicateKeyErrorCode = 11000;
    }
}