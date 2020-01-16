namespace NServiceBus.Storage.MongoDB
{
    using System;
    using Features;
    using global::MongoDB.Driver;
    using global::MongoDB.Driver.Core.Clusters;

    class SynchronizedStorage : Feature
    {
        protected override void Setup(FeatureConfigurationContext context)
        {
            var client = context.Settings.Get<Func<IMongoClient>>(SettingsKeys.MongoClient)();
            var databaseName = context.Settings.Get<string>(SettingsKeys.DatabaseName);
            var collectionNamingConvention = context.Settings.Get<Func<Type, string>>(SettingsKeys.CollectionNamingConvention);

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
                        var clusterType = client.Cluster.Description.Type;

                        //HINT: cluster configuration check is needed as the built-in checks, executed during "StartTransaction() call,
                        //      do not detect if the cluster configuration is a supported one. Only the version ranges are validated.
                        //      Without this check, exceptions will be thrown during message processing.
                        if (clusterType != ClusterType.ReplicaSet && clusterType != ClusterType.Sharded)
                        {
                            throw new Exception($"Transactions are only supported on replica sets or sharded clusters. Disable support for transactions by calling 'EndpointConfiguration.UsePersistence<{nameof(MongoPersistence)}>().UseTransactions(false)'.");
                        }

                        try
                        {
                            session.StartTransaction();
                            session.AbortTransaction();
                        }
                        catch (NotSupportedException ex)
                        {
                            throw new Exception($"Transactions are not supported by the MongoDB server. Disable support for transactions by calling 'EndpointConfiguration.UsePersistence<{nameof(MongoPersistence)}>().UseTransactions(false)'.", ex);
                        }
                    }
                }
            }
            catch (NotSupportedException ex)
            {
                throw new Exception("Sessions are not supported by the MongoDB server. The NServiceBus.Storage.MongoDB persistence requires MongoDB server version 3.6 or greater.", ex);
            }
            catch (TimeoutException ex)
            {
                throw new Exception("Unable to connect to the MongoDB server. Check the connection settings, and verify the server is running and accessible.", ex);
            }

            context.Container.ConfigureComponent(() => new StorageSessionFactory(client, useTransactions, databaseName, collectionNamingConvention, MongoPersistence.DefaultTransactionTimeout), DependencyLifecycle.SingleInstance);
            context.Container.ConfigureComponent<StorageSessionAdapter>(DependencyLifecycle.SingleInstance);
        }
    }
}
