using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Driver;
using NServiceBus.Extensibility;
using NServiceBus.Logging;
using NServiceBus.Unicast.Subscriptions;
using NServiceBus.Unicast.Subscriptions.MessageDrivenSubscriptions;

namespace NServiceBus.Storage.MongoDB
{
    class SubscriptionPersister : ISubscriptionStorage
    {
        public SubscriptionPersister(IMongoCollection<EventSubscription> subscriptionsCollection)
        {
            this.subscriptionsCollection = subscriptionsCollection;
        }

        public Task Subscribe(Subscriber subscriber, MessageType messageType, ContextBag context)
        {
            var subscription = new EventSubscription
            {
                MessageTypeName = messageType.TypeName,
                TransportAddress = subscriber.TransportAddress,
                Endpoint = subscriber.Endpoint
            };

            if (IsLegacySubscription(subscription))
            {
                // support for older versions of NServiceBus which do not provide a logical endpoint name. We do not want to replace a non-null value with null.
                return AddLegacySubscription(subscription);
            }

            return AddOrUpdateSubscription(subscription);
        }

        public async Task Unsubscribe(Subscriber subscriber, MessageType messageType, ContextBag context)
        {
            var filter = filterBuilder.And(
                filterBuilder.Eq(s => s.MessageTypeName, messageType.TypeName),
                filterBuilder.Eq(s => s.TransportAddress, subscriber.TransportAddress));
            var result = await subscriptionsCollection.DeleteManyAsync(filter).ConfigureAwait(false);

            Log.DebugFormat("Deleted {0} subscriptions for address '{1}' on message type '{2}'", result.DeletedCount, subscriber.TransportAddress, messageType.TypeName);
        }

        public async Task<IEnumerable<Subscriber>> GetSubscriberAddressesForMessage(IEnumerable<MessageType> messageTypes, ContextBag context)
        {
            var messageTypeNames = new List<string>();
            foreach (var messageType in messageTypes)
            {
                messageTypeNames.Add(messageType.TypeName);
            }
            var filter = filterBuilder.In(s => s.MessageTypeName, messageTypeNames);
            // This projection allows a covered query:
            var projection = Builders<EventSubscription>.Projection
                .Include(s => s.TransportAddress)
                .Include(s => s.Endpoint)
                .Exclude("_id");

            // == Following is used to view index usage for the query ==
            //var options = new FindOptions();
            //options.Modifiers = new global::MongoDB.Bson.BsonDocument("$explain", true);
            //var queryStats = await subscriptionsCollection
            //    .WithReadConcern(ReadConcern.Default)
            //    .Find(filter, options)
            //    .Project(projection)
            //    .ToListAsync()
            //    .ConfigureAwait(false);
            // =========================================================

            var result = await subscriptionsCollection
                .Find(filter)
                .Project(projection)
                .ToListAsync()
                .ConfigureAwait(false);

            return result.Select(r => new Subscriber(
                r[nameof(EventSubscription.TransportAddress)].AsString,
                r[nameof(EventSubscription.Endpoint)].IsBsonNull ? null : r[nameof(EventSubscription.Endpoint)].AsString));
        }

        static bool IsLegacySubscription(EventSubscription subscription) => subscription.Endpoint == null;

        async Task AddLegacySubscription(EventSubscription subscription)
        {
            try
            {
                await subscriptionsCollection.InsertOneAsync(subscription).ConfigureAwait(false);
                Log.DebugFormat("Created legacy subscription for '{0}' on '{1}'", subscription.TransportAddress, subscription.MessageTypeName);
            }
            catch (MongoWriteException e) when (e.WriteError?.Code == DuplicateKeyErrorCode)
            {
                // duplicate key error which means a document already exists
                // existing subscriptions should not be stripped of their logical endpoint name
                Log.DebugFormat("Skipping legacy subscription for '{0}' on '{1}' because a newer subscription already exists", subscription.TransportAddress, subscription.MessageTypeName);
            }
        }

        async Task AddOrUpdateSubscription(EventSubscription subscription)
        {
            try
            {
                var filter = filterBuilder.And(
                    filterBuilder.Eq(s => s.MessageTypeName, subscription.MessageTypeName),
                    filterBuilder.Eq(s => s.TransportAddress, subscription.TransportAddress));
                var update = Builders<EventSubscription>.Update.Set(s => s.Endpoint, subscription.Endpoint);
                var options = new UpdateOptions {IsUpsert = true};

                var result = await subscriptionsCollection.UpdateOneAsync(filter, update, options).ConfigureAwait(false);
                if (result.ModifiedCount > 0)
                {
                    // ModifiedCount is also 0 when the update values match exactly the existing document.
                    Log.DebugFormat("Updated existing subscription of '{0}' on '{1}'", subscription.TransportAddress, subscription.MessageTypeName);
                }
                else if (result.UpsertedId != null)
                {
                    Log.DebugFormat("Created new subscription for '{0}' on '{1}'", subscription.TransportAddress, subscription.MessageTypeName);
                }
            }
            catch (MongoWriteException e) when (e.WriteError?.Code == DuplicateKeyErrorCode)
            {
                // This is thrown when there is a race condition and the same subscription has been added already.
                // As upserts create new documents, those operations aren't atomic in regards to concurrent upserts
                // and duplicate documents will only be prevented by the unique key constraint.
            }
        }

        public void CreateIndexes()
        {
            var uniqueIndex = new CreateIndexModel<EventSubscription>(Builders<EventSubscription>.IndexKeys
                    .Ascending(x => x.MessageTypeName)
                    .Ascending(x => x.TransportAddress),
                new CreateIndexOptions
                { Unique = true });
            var searchIndex = new CreateIndexModel<EventSubscription>(Builders<EventSubscription>.IndexKeys
                .Ascending(x => x.MessageTypeName)
                .Ascending(x => x.TransportAddress)
                .Ascending(x => x.Endpoint));
            subscriptionsCollection.Indexes.CreateMany(new[] { uniqueIndex, searchIndex });
        }

        IMongoCollection<EventSubscription> subscriptionsCollection;
        const int DuplicateKeyErrorCode = 11000;
        static readonly ILog Log = LogManager.GetLogger<SubscriptionPersister>();
        static readonly FilterDefinitionBuilder<EventSubscription> filterBuilder = Builders<EventSubscription>.Filter;
    }
}