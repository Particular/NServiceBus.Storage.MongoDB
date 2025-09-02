namespace NServiceBus.Storage.MongoDB;

using global::MongoDB.Driver;

sealed class DefaultMongoClientProvider : IMongoClientProvider
{
    public IMongoClient Client
    {
        get
        {
            client ??= new MongoClient();
            return client;
        }
    }

    MongoClient? client;
}