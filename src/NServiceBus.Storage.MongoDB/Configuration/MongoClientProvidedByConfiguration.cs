namespace NServiceBus.Storage.MongoDB;

using global::MongoDB.Driver;

class MongoClientProvidedByConfiguration(IMongoClient client) : IMongoClientProvider
{
    public IMongoClient Client { get; } = client;
}