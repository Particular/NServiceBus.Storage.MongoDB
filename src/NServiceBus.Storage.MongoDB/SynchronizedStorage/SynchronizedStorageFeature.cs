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

            context.Container.ConfigureComponent(() => new SynchronizedStorage(client, databaseName, collectionNamingScheme), DependencyLifecycle.SingleInstance);
            context.Container.ConfigureComponent<SynchronizedStorageAdapter>(DependencyLifecycle.SingleInstance);
        }
    }
}
