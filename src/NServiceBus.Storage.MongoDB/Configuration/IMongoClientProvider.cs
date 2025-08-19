namespace NServiceBus.Storage.MongoDB;

using global::MongoDB.Driver;

/// <summary>
/// Provides a mongo client via dependency injection. A custom implementation can be registered on the container and will be picked up by the persistence.
/// <remarks>
/// The client provided will not be disposed by the persistence. It is the responsibility of the provider to take care of proper resource disposal if necessary.
/// </remarks>
/// </summary>
public interface IMongoClientProvider
{
    /// <summary>
    /// The mongo client to use.
    /// </summary>
    IMongoClient Client { get; }
}