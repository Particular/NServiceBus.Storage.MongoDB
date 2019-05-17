using MongoDB.Driver;

namespace NServiceBus.Storage.MongoDB
{
    using System;
    using NServiceBus.Features;
    using NServiceBus.ObjectBuilder;

    class ConfigureMongoDBPersistence : Feature
    {
        protected override void Setup(FeatureConfigurationContext context)
        {
            context.Settings.TryGet(SettingsKeys.ConnectionString, out string connectionString);

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new Exception("Cannot configure Mongo Persister. No connection string was found");
            }

            var databaseName = MongoUrl.Create(connectionString).DatabaseName;
            if (string.IsNullOrWhiteSpace(databaseName))
            {
                throw new Exception("Cannot configure Mongo Persister. Database name not present in the connection string.");
            }

            var client = new MongoClient(connectionString);
            var database = client.GetDatabase(databaseName);

            if (database == null)
            {
                throw new Exception(nameof(database));
            }

            context.Container.RegisterSingleton(database);
        }
    }
}