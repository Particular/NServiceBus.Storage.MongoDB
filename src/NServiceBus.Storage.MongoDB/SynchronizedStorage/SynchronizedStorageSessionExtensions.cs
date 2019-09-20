namespace NServiceBus
{
    using System;
    using MongoDB.Driver;
    using Persistence;
    using Storage.MongoDB;

    /// <summary>
    /// MongoDB persistence specific extension methods for the <see cref="SynchronizedStorageSession"/>.
    /// </summary>
    public static class SynchronizedStorageSessionExtensions
    {
        /// <summary>
        /// Retrieves the current MongoDB client session from the context.
        /// </summary>
        public static IClientSessionHandle GetClientSession(this SynchronizedStorageSession session)
        {
            Guard.AgainstNull(nameof(session), session);

            if (session is IExposeAMongoSession storageSession)
            {
                return storageSession.MongoSession;
            }

            throw new Exception($"Cannot access the synchronized storage session. Ensure that 'EndpointConfiguration.UsePersistence<{nameof(MongoPersistence)}>()' has been called.");
        }
    }
}