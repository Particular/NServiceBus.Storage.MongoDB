namespace NServiceBus.Storage.MongoDB
{
    interface IMongoSessionProvider
    {
        global::MongoDB.Driver.IClientSessionHandle MongoSession { get; }
    }
}
