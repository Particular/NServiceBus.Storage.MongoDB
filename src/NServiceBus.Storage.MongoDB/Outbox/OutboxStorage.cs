using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Options;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using NServiceBus.Features;
using NServiceBus.Outbox;

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
            if (!context.Settings.TryGet(SettingsKeys.CollectionNamingConvention, out Func<Type, string> collectionNamingConvention))
            {
                collectionNamingConvention = type => type.Name.ToLower();
            }

            var client = context.Settings.Get<Func<IMongoClient>>(SettingsKeys.MongoClient)();
            var databaseName = context.Settings.Get<string>(SettingsKeys.DatabaseName);

            if (!BsonClassMap.IsClassMapRegistered(typeof(TransportOperation)))
            {
                BsonClassMap.RegisterClassMap<TransportOperation>(cm =>
                {
                    cm.AutoMap();
                    cm.MapMember(c => c.Headers).SetSerializer(new DictionaryInterfaceImplementerSerializer<Dictionary<string, string>>(DictionaryRepresentation.ArrayOfDocuments));
                    cm.MapMember(c => c.Options).SetSerializer(new DictionaryInterfaceImplementerSerializer<Dictionary<string, string>>(DictionaryRepresentation.ArrayOfDocuments));
                });
            }

            context.Container.ConfigureComponent(() => new OutboxPersister(client, databaseName, collectionNamingConvention), DependencyLifecycle.SingleInstance); //TODO
        }
    }
}
