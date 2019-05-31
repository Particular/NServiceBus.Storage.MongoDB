using System;
using MongoDB.Driver;
using NServiceBus.Persistence;
using NServiceBus.Storage.MongoDB;

namespace NServiceBus
{
    /// <summary>
    ///
    /// </summary>
    public static class SynchronizedStorageSessionExtensions
    {
        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="session"></param>
        /// <param name="name"></param>
        /// <param name="settings"></param>
        /// <returns></returns>
        public static IMongoCollection<T> GetCollection<T>(this SynchronizedStorageSession session, string name, MongoCollectionSettings settings = null)
        {
            Guard.AgainstNull(nameof(session), session);
            Guard.AgainstNullAndEmpty(nameof(name), name);

            if (session is StorageSession storageSession)
            {
                return storageSession.GetCollection<T>(name, settings);
            }

            throw new Exception($"Cannot access the synchronized storage session. Ensure that 'EndpointConfiguration.UsePersistence<{nameof(MongoPersistence)}>()' has been called.");
        }
    }
}
