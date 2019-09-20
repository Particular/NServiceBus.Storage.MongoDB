namespace NServiceBus.Storage.MongoDB
{
    interface IExposeAMongoSession
    {
        global::MongoDB.Driver.IClientSessionHandle MongoSession { get; }
    }
}
