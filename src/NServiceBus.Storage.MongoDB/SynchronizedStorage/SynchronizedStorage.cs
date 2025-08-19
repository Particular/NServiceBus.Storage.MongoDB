namespace NServiceBus.Storage.MongoDB;

using System;
using System.Threading;
using System.Threading.Tasks;
using Features;
using global::MongoDB.Driver.Core.Clusters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Persistence;

class SynchronizedStorage : Feature
{
    public SynchronizedStorage() =>
        // Depends on the core feature
        DependsOn<Features.SynchronizedStorage>();

    protected override void Setup(FeatureConfigurationContext context)
    {
        // In case the persistence is used without the SynchronizedStorage feature, we still need to try to register the IMongoClientProvider
        context.Services.TryAddSingleton(context.Settings.Get<IMongoClientProvider>());

        var databaseName = context.Settings.Get<string>(SettingsKeys.DatabaseName);
        var collectionNamingConvention = context.Settings.Get<Func<Type, string>>(SettingsKeys.CollectionNamingConvention);

        if (!context.Settings.TryGet(SettingsKeys.UseTransactions, out bool useTransactions))
        {
            useTransactions = true;
        }

        context.Settings.AddStartupDiagnosticsSection("NServiceBus.Storage.MongoDB.StorageSession", new
        {
            UseTransaction = useTransactions
        });

        context.RegisterStartupTask(sp => new VerifyClusterDetails(sp.GetRequiredService<IMongoClientProvider>(), databaseName, useTransactions));

        context.Services.AddScoped<ICompletableSynchronizedStorageSession, SynchronizedStorageSession>();
        context.Services.AddScoped(sp => (sp.GetService<ICompletableSynchronizedStorageSession>() as IMongoSynchronizedStorageSession)!);
        context.Services.AddSingleton(sp => new StorageSessionFactory(sp.GetRequiredService<IMongoClientProvider>().Client, useTransactions, databaseName, collectionNamingConvention, MongoPersistence.DefaultTransactionTimeout));
    }

    class VerifyClusterDetails(IMongoClientProvider clientProvider, string databaseName, bool useTransactions) : FeatureStartupTask
    {
        protected override async Task OnStart(IMessageSession session, CancellationToken cancellationToken = default)
        {
            try
            {
                var client = clientProvider.Client;
                var database = client.GetDatabase(databaseName);

                // perform a query to the server to make sure cluster details are loaded
                await database.ListCollectionNamesAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

                using var mongoSession = await client.StartSessionAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

                if (useTransactions)
                {
                    var clusterType = client.Cluster.Description.Type;

                    //HINT: cluster configuration check is needed as the built-in checks, executed during "StartTransaction() call,
                    //      do not detect if the cluster configuration is a supported one. Only the version ranges are validated.
                    //      Without this check, exceptions will be thrown during message processing.
                    if (clusterType is not ClusterType.ReplicaSet and not ClusterType.Sharded)
                    {
                        throw new Exception(
                            $"The cluster type in use is {clusterType}, but transactions are only supported on replica sets or sharded clusters. Disable support for transactions by calling 'EndpointConfiguration.UsePersistence<{nameof(MongoPersistence)}>().UseTransactions(false)'.");
                    }

                    try
                    {
                        mongoSession.StartTransaction();
                        await mongoSession.AbortTransactionAsync(cancellationToken).ConfigureAwait(false);
                    }
                    catch (NotSupportedException ex)
                    {
                        throw new Exception(
                            $"Transactions are not supported by the MongoDB server. Disable support for transactions by calling 'EndpointConfiguration.UsePersistence<{nameof(MongoPersistence)}>().UseTransactions(false)'.",
                            ex);
                    }
                }
            }
            catch (ArgumentException ex)
            {
                throw new Exception(
                    $"The persistence database name '{databaseName}' is invalid. Configure a valid database name by calling 'EndpointConfiguration.UsePersistence<{nameof(MongoPersistence)}>().DatabaseName(databaseName)'.",
                    ex);
            }
            catch (NotSupportedException ex)
            {
                throw new Exception(
                    "Sessions are not supported by the MongoDB server. The NServiceBus.Storage.MongoDB persistence requires MongoDB server version 3.6 or greater.",
                    ex);
            }
            catch (TimeoutException ex)
            {
                throw new Exception(
                    "Unable to connect to the MongoDB server. Check the connection settings, and verify the server is running and accessible.",
                    ex);
            }
        }

        protected override Task OnStop(IMessageSession session, CancellationToken cancellationToken = default)
        {
            if (clientProvider is DefaultMongoClientProvider { Client: { } client })
            {
                client.Dispose();
            }
            return Task.CompletedTask;
        }
    }
}