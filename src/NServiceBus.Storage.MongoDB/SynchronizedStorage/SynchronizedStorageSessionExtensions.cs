using System;
using MongoDB.Driver;
using NServiceBus.Persistence;
using NServiceBus.Storage.MongoDB;

namespace NServiceBus
{
    public static class SynchronizedStorageSessionExtensions
    {
        public static IMongoCollection<T> GetCollection<T>(this SynchronizedStorageSession session, string name, MongoCollectionSettings settings = null)
        {
            Guard.AgainstNull(nameof(session), session);
            Guard.AgainstNullAndEmpty(nameof(name), name);

            if (session is StorageSession storageSession)
            {
                return storageSession.GetCollection<T>(name, settings);
            }

            throw new Exception("Cannot access the MongoDB synchronized storage session. Ensure that the endpoint has been configued to use MongoDB persistence.");
        }
    }
}
