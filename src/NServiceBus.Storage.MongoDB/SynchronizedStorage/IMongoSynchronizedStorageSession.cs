namespace NServiceBus.Storage.MongoDB;

using global::MongoDB.Driver;

/// <summary>
/// Exposes the <see cref="IClientSessionHandle"/> managed by NServiceBus.
/// </summary>
public interface IMongoSynchronizedStorageSession
{
    /// <summary>
    /// Provides access to the <see cref="IClientSessionHandle"/>
    /// </summary>
    IClientSessionHandle? MongoSession { get; }
}