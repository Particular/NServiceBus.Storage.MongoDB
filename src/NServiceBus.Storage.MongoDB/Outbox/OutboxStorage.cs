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
        Defaults(s => s.EnableFeatureByDefault<SynchronizedStorage>());

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

        if (!context.Settings.TryGet<Func<IMongoClient>>(SettingsKeys.MongoClient, out Func<IMongoClient>? client))
        {
            return;
        }

        var databaseName = context.Settings.Get<string>(SettingsKeys.DatabaseName);
        var collectionNamingConvention = context.Settings.Get<Func<Type, string>>(SettingsKeys.CollectionNamingConvention);

        context.Services.AddSingleton<IOutboxStorage>(new OutboxPersister(client(), databaseName, collectionNamingConvention));

        RegisterOutboxClassMappings();
    }

    internal static void RegisterOutboxClassMappings()
    {
        if (BsonClassMap.IsClassMapRegistered(typeof(StorageTransportOperation)))
        {
            return;
        }

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
}