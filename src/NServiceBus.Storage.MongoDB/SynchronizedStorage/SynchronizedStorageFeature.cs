using System;
using MongoDB.Driver;
using NServiceBus.Features;

namespace NServiceBus.Storage.MongoDB
{
    class SynchronizedStorageFeature : Feature
    {
        public SynchronizedStorageFeature()
        {
            DependsOn<ConfigureMongoDBPersistence>();
        }

        protected override void Setup(FeatureConfigurationContext context)
        {
            if (!context.Settings.TryGet(SettingsKeys.CollectionNamingScheme, out Func<Type, string> collectionNamingScheme))
            {
                collectionNamingScheme = type => type.Name.ToLower();
            }

            context.Container.ConfigureComponent(builder => new SynchronizedStorage(builder.Build<IMongoDatabase>(), collectionNamingScheme), DependencyLifecycle.SingleInstance);
            context.Container.ConfigureComponent<SynchronizedStorageAdapter>(DependencyLifecycle.SingleInstance);
        }
    }
}
