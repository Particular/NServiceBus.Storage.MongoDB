namespace NServiceBus.Storage.MongoDB.Testing
{
    using global::MongoDB.Driver;
    using NServiceBus.Persistence;

    /// <summary>
    /// A fake implementation for <see cref="SynchronizedStorageSession"/> for testing purposes.
    /// </summary>
    public class TestableSynchronizedStorageSession : SynchronizedStorageSession, IExposeAMongoSession
    {
        /// <summary>
        /// Creates a new instance of <see cref="TestableSynchronizedStorageSession"/> using the provided <see cref="IClientSessionHandle"/>.
        /// </summary>
        /// <param name="clientSessionHandle"></param>
        public TestableSynchronizedStorageSession(IClientSessionHandle clientSessionHandle)
        {
            MongoSession = clientSessionHandle;
        }

        /// <summary>
        /// The client session handle which is retrieved by calling <see cref="SynchronizedStorageSessionExtensions.GetClientSession"/>.
        /// </summary>
        public IClientSessionHandle MongoSession { get; }
    }
}
