namespace NServiceBus.Storage.MongoDB
{
    using global::MongoDB.Driver;

    interface IMongoSessionProvider
    {
        IClientSessionHandle MongoSession { get; }
    }
}
