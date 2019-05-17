using MongoDB.Driver;

namespace NServiceBus.Storage.MongoDB
{
    using System;
    using NServiceBus.ObjectBuilder;

    static class ConfigureMongoDBPersistence
    {
        public static IConfigureComponents MongoDBPersistence(this IConfigureComponents config, Func<string> getConnectionString)
        {
            var connectionString = getConnectionString();

            if (String.IsNullOrWhiteSpace(connectionString))
            {
                throw new Exception("Cannot configure Mongo Persister. No connection string was found");
            }

            return MongoPersistenceWithConectionString(config, connectionString);
        }

        static IConfigureComponents MongoPersistenceWithConectionString(IConfigureComponents config, string connectionString)
        {
            var databaseName = MongoUrl.Create(connectionString).DatabaseName;
            if (String.IsNullOrWhiteSpace(databaseName))
            {
                throw new Exception("Cannot configure Mongo Persister. Database name not present in the connection string.");
            }

            var client = new MongoClient(connectionString);
            var database = client.GetDatabase(databaseName);

            return MongoDBPersistence(config, database);
        }

        static IConfigureComponents MongoDBPersistence(this IConfigureComponents config, IMongoDatabase database)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            if (database == null) throw new ArgumentNullException(nameof(database));

            config.RegisterSingleton(database);

            return config;
        }
    }
}