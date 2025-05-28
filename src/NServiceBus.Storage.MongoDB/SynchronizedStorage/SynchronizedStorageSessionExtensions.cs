namespace NServiceBus;

using System;
using MongoDB.Driver;
using Persistence;
using Storage.MongoDB;

/// <summary>
/// MongoDB persistence specific extension methods for the <see cref="ISynchronizedStorageSession"/>.
/// </summary>
public static class SynchronizedStorageSessionExtensions
{
    /// <summary>
    /// Retrieves the current MongoDB client session from the context.
    /// </summary>
    public static IClientSessionHandle GetClientSession(this ISynchronizedStorageSession session) =>
        session.MongoPersistenceSession().MongoSession!;

    /// <summary>
    /// Retrieves the shared <see cref="IMongoSynchronizedStorageSession"/> from the <see cref="SynchronizedStorageSession"/>.
    /// </summary>
    public static IMongoSynchronizedStorageSession MongoPersistenceSession(this ISynchronizedStorageSession session)
    {
        ArgumentNullException.ThrowIfNull(session);

        if (session is IMongoSynchronizedStorageSession mongoSession)
        {
            return mongoSession;
        }

        throw new Exception(
            $"Cannot access the synchronized storage session. Ensure that 'EndpointConfiguration.UsePersistence<{nameof(MongoPersistence)}>()' has been called.");
    }
}