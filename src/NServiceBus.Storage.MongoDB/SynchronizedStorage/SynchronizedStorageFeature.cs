using System;
using MongoDB.Driver;
using NServiceBus.Features;

namespace NServiceBus.Storage.MongoDB
{
    class SynchronizedStorageFeature : Feature
    {
        protected override void Setup(FeatureConfigurationContext context)
        {
            if (!context.Settings.TryGet(SettingsKeys.CollectionNamingScheme, out Func<Type, string> collectionNamingScheme))
            {
                collectionNamingScheme = type => type.Name.ToLower();
            }

            var client = context.Settings.Get<Func<IMongoClient>>(SettingsKeys.Client)();
            var databaseName = context.Settings.Get<string>(SettingsKeys.DatabaseName);

            if (!context.Settings.TryGet(SettingsKeys.UseTransactions, out bool useTransactions))
            {
                useTransactions = true;
            }

            if (useTransactions)
            {
                try
                {
                    using (var session = client.StartSession())
                    {
                        session.StartTransaction();
                        session.AbortTransaction();
                    }
                }
                catch (NotSupportedException ex)
                {
                    throw new Exception("error message", ex);
                }
            }

            context.Container.ConfigureComponent(() => new SynchronizedStorage(client, useTransactions, databaseName, collectionNamingScheme), DependencyLifecycle.SingleInstance);
            context.Container.ConfigureComponent<SynchronizedStorageAdapter>(DependencyLifecycle.SingleInstance);
        }
    }
}
