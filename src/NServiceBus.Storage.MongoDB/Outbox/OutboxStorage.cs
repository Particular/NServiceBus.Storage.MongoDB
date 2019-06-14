using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Driver;
using NServiceBus.Features;

namespace NServiceBus.Storage.MongoDB
{
    class OutboxStorage : Feature
    {
        OutboxStorage()
        {
            DependsOn<Features.Outbox>();
            DependsOn<SynchronizedStorage>();
        }

        protected override void Setup(FeatureConfigurationContext context)
        {
            var client = context.Settings.Get<Func<IMongoClient>>(SettingsKeys.MongoClient)();
            var databaseName = context.Settings.Get<string>(SettingsKeys.DatabaseName);

            context.Container.ConfigureComponent(() => new OutboxPersister(client, databaseName), DependencyLifecycle.SingleInstance); //TODO
        }
    }
}
