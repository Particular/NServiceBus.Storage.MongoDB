using System;
using MongoDB.Driver;
using NServiceBus.Features;

namespace NServiceBus.Storage.MongoDB
{
    class SynchronizedStorage : Feature
    {
        protected override void Setup(FeatureConfigurationContext context)
        {
            if (!context.Settings.TryGet(SettingsKeys.CollectionNamingConvention, out Func<Type, string> collectionNamingConvention))
            {
                collectionNamingConvention = type => type.Name.ToLower();
            }

            var client = context.Settings.Get<Func<IMongoClient>>(SettingsKeys.MongoClient)();
            var databaseName = context.Settings.Get<string>(SettingsKeys.DatabaseName);

            if (!context.Settings.TryGet(SettingsKeys.UseTransactions, out bool useTransactions))
            {
                useTransactions = true;
            }

            try
            {
                client.GetDatabase(databaseName);
            }
            catch (ArgumentException ex)
            {
                throw new Exception($"The persistence database name '{databaseName}' is invalid. Configure a valid database name by calling 'EndpointConfiguration.UsePersistence<{nameof(MongoPersistence)}>().DatabaseName(databaseName)'.", ex);
            }

            try
            {
                using (var session = client.StartSession())
                {
                    if (useTransactions)
                    {
                        try
                        {
                            session.StartTransaction();
                            session.AbortTransaction();
                        }
                        catch (NotSupportedException ex)
                        {
                            throw new Exception("Transactions are not supported by the MongoDB server/cluster. Disable support for transactions by calling the 'persistence.UseTransactions(false)' API.", ex);
                        }
                    }
                }
            }
            catch (TimeoutException ex)
            {
                throw new Exception("Unable to connect to MongoDB. Check your connection settings and verify MongoDB is running and accessible.", ex);
            }

            context.Container.ConfigureComponent(() => new SynchronizedStorageFactory(client, useTransactions, databaseName, collectionNamingConvention), DependencyLifecycle.SingleInstance);
            context.Container.ConfigureComponent<SynchronizedStorageAdapter>(DependencyLifecycle.SingleInstance);
        }
    }
}
