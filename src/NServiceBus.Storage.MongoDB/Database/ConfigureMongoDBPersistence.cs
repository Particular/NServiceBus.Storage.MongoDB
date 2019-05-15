using MongoDB.Driver;

namespace NServiceBus.Storage.MongoDB
{
    using System;
    using System.Configuration;
    using NServiceBus.ObjectBuilder;

    static class ConfigureMongoDBPersistence
    {
        public static IConfigureComponents MongoDBPersistence(this IConfigureComponents config, IMongoDatabase database)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            if (database == null) throw new ArgumentNullException(nameof(database));

            config.RegisterSingleton(database);

            return config;
        }

        public static IConfigureComponents MongoDBPersistence(this IConfigureComponents config, string connectionStringName)
        {
            var connectionStringEntry = ConfigurationManager.ConnectionStrings[connectionStringName];

            if (connectionStringEntry == null)
            {
                throw new ConfigurationErrorsException(string.Format("Cannot configure Mongo Persister. No connection string named {0} was found", connectionStringName));
            }

            var connectionString = connectionStringEntry.ConnectionString;

            return MongoPersistenceWithConectionString(config, connectionString);
        }

        public static IConfigureComponents MongoDBPersistence(this IConfigureComponents config)
        {
            return MongoDBPersistence(config, ConnectionStringNames.DefaultConnectionStringName);
        }

        public static IConfigureComponents MongoPersistenceWithConectionString(IConfigureComponents config, string connectionString)
        {
            var databaseName = MongoUrl.Create(connectionString).DatabaseName;
            if (String.IsNullOrWhiteSpace(databaseName))
            {
                throw new ConfigurationErrorsException("Cannot configure Mongo Persister. Database name not present in the connection string.");
            }

            var client = new MongoClient(connectionString);
            var database = client.GetDatabase(databaseName);

            return MongoDBPersistence(config, database);
        }

        public static IConfigureComponents MongoDBPersistence(this IConfigureComponents config, Func<string> getConnectionString)
        {
            var connectionString = getConnectionString();

            if (String.IsNullOrWhiteSpace(connectionString))
            {
                throw new ConfigurationErrorsException("Cannot configure Mongo Persister. No connection string was found");
            }

            return MongoPersistenceWithConectionString(config, connectionString);
        }
    }
}