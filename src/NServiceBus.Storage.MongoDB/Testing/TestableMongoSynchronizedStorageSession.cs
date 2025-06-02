namespace NServiceBus.Testing;

using MongoDB.Driver;
using Persistence;
using Storage.MongoDB;

/// <summary>
/// A fake implementation for <see cref="ISynchronizedStorageSession"/> for testing purposes.
/// </summary>
public class TestableMongoSynchronizedStorageSession : ISynchronizedStorageSession, IMongoSynchronizedStorageSession
{
    /// <summary>
    /// Creates a new instance of <see cref="TestableMongoSynchronizedStorageSession"/> using the provided <see cref="IClientSessionHandle"/>.
    /// </summary>
    /// <param name="clientSessionHandle">Handle to the MongoDB client session</param>
    public TestableMongoSynchronizedStorageSession(IClientSessionHandle clientSessionHandle) => MongoSession = clientSessionHandle;

    /// <summary>
    /// The client session handle which is retrieved by calling <see cref="SynchronizedStorageSessionExtensions.GetClientSession"/>.
    /// </summary>
    public IClientSessionHandle MongoSession { get; }
}