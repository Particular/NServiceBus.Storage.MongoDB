namespace NServiceBus.Storage.MongoDB;

using System;
using System.Collections.Generic;
using Features;
using global::MongoDB.Bson.Serialization;
using global::MongoDB.Bson.Serialization.Options;
using global::MongoDB.Bson.Serialization.Serializers;
using global::MongoDB.Driver;
using Microsoft.Extensions.DependencyInjection;
using Outbox;

class OutboxStorage : Feature
{
    OutboxStorage()
    {
        Defaults(s =>
        {
            s.SetDefault(new OutboxPersistenceConfiguration());
            s.EnableFeatureByDefault<SynchronizedStorage>();
        });

        DependsOn<Outbox>();
        DependsOn<SynchronizedStorage>();
    }

    protected override void Setup(FeatureConfigurationContext context)
    {
        if (context.Settings.TryGet(SettingsKeys.UseTransactions, out bool useTransactions) && useTransactions == false)
        {
            throw new Exception(
                $"Transactions are required when the Outbox is enabled, but they have been disabled by calling 'EndpointConfiguration.UsePersistence<{nameof(MongoPersistence)}>().UseTransactions(false)'.");
        }

        var databaseName = context.Settings.Get<string>(SettingsKeys.DatabaseName);
        var collectionNamingConvention = context.Settings.Get<Func<Type, string>>(SettingsKeys.CollectionNamingConvention);
        var databaseSettings = context.Settings.Get<MongoDatabaseSettings>();
        var collectionSettings = context.Settings.Get<MongoCollectionSettings>();
        var configuration = context.Settings.Get<OutboxPersistenceConfiguration>();
        var endpointName = context.Settings.EndpointName();

        context.Services.AddSingleton<IOutboxStorage>(sp => new OutboxPersister(sp.GetRequiredService<IMongoClientProvider>().Client, endpointName, configuration.ReadFallbackEnabled, databaseName, databaseSettings, collectionNamingConvention, collectionSettings));

        var usesDefaultClassMap = RegisterOutboxClassMappings();

        context.Settings.AddStartupDiagnosticsSection("NServiceBus.Storage.MongoDB.Outbox", new
        {
            UsesDefaultClassMap = usesDefaultClassMap,
#pragma warning disable IDE0037
            TimeToKeepDeduplicationData = configuration.TimeToKeepDeduplicationData,
#pragma warning restore IDE0037
        });
    }

    internal static bool RegisterOutboxClassMappings()
    {
        if (!BsonClassMap.IsClassMapRegistered(typeof(OutboxRecordId)))
        {
            BsonClassMap.RegisterClassMap<OutboxRecordId>(cm =>
            {
                cm.AutoMap();
                cm.MapMember(x => x.PartitionKey).SetElementName("pk");
                cm.MapMember(x => x.MessageId).SetElementName("mid");
            });
        }

        if (!BsonClassMap.IsClassMapRegistered(typeof(OutboxRecord)))
        {
            BsonClassMap.RegisterClassMap<OutboxRecord>(cm =>
            {
                cm.AutoMap();
                cm.MapIdMember(x => x.Id).SetSerializer(new OutboxRecordIdSerializer());
            });
        }

        if (!BsonClassMap.IsClassMapRegistered(typeof(StorageTransportOperation)))
        {
            BsonClassMap.RegisterClassMap<StorageTransportOperation>(cm =>
            {
                cm.AutoMap();
                cm.MapMember(c => c.Headers)
                    .SetSerializer(
                        new DictionaryInterfaceImplementerSerializer<Dictionary<string, string>>(
                            DictionaryRepresentation.ArrayOfDocuments));
                cm.MapMember(c => c.Options)
                    .SetSerializer(
                        new DictionaryInterfaceImplementerSerializer<Dictionary<string, string>>(
                            DictionaryRepresentation.ArrayOfDocuments));
            });
        }

        return true;
    }
}