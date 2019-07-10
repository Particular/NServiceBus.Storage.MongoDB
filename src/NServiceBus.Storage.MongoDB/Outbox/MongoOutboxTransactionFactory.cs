using System;
using System.Threading.Tasks;
using MongoDB.Driver;
using NServiceBus.Extensibility;
using NServiceBus.Outbox;

namespace NServiceBus.Storage.MongoDB
{
    class MongoOutboxTransactionFactory
    {
        public MongoOutboxTransactionFactory(IMongoClient client, string databaseName, Func<Type, string> collectionNamingConvention)
        {
            this.client = client;
            this.databaseName = databaseName;
            this.collectionNamingConvention = collectionNamingConvention;
        }

        public async Task<OutboxTransaction> BeginTransaction(ContextBag context)
        {
            var mongoSession = await client.StartSessionAsync().ConfigureAwait(false);

            mongoSession.StartTransaction(new TransactionOptions(ReadConcern.Majority, ReadPreference.Primary, WriteConcern.WMajority));

            return new MongoOutboxTransaction(mongoSession, databaseName, context, collectionNamingConvention);
        }

        readonly IMongoClient client;
        readonly string databaseName;
        readonly Func<Type, string> collectionNamingConvention;
    }
}
