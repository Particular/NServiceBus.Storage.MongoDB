using MongoDB.Driver;

namespace NServiceBus.Storage.MongoDB
{
    /// <summary>
    /// Interface that allows testing of the SynchronizedStorageSessionExtensions in a handler
    /// </summary>
    public interface IHaveAMongoSession
    {
        /// <summary>
        /// Provides access to the underlying IClientSessionHandle
        /// </summary>
        IClientSessionHandle MongoSession { get; }
    }
}
