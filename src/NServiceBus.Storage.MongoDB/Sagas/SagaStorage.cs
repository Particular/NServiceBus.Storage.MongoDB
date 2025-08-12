namespace NServiceBus.Storage.MongoDB;

using System;
using Features;
using global::MongoDB.Driver;
using Microsoft.Extensions.DependencyInjection;
using Sagas;

class SagaStorage : Feature
{
    SagaStorage()
    {
        Defaults(s => s.EnableFeatureByDefault<SynchronizedStorage>());

        DependsOn<Sagas>();
        DependsOn<SynchronizedStorage>();
    }

    protected override void Setup(FeatureConfigurationContext context)
    {
        if (!context.Settings.TryGet(SettingsKeys.VersionElementName, out string versionElementName))
        {
            versionElementName = SagaPersister.DefaultVersionElementName;
        }

        if (!context.Settings.TryGet<Func<IMongoClient>>(SettingsKeys.MongoClient, out Func<IMongoClient>? client))
        {
            return;
        }

        string? databaseName = context.Settings.Get<string>(SettingsKeys.DatabaseName);
        Func<Type, string>? collectionNamingConvention =
            context.Settings.Get<Func<Type, string>>(SettingsKeys.CollectionNamingConvention);

        var collectionSettings = new MongoCollectionSettings
        {
            ReadConcern = ReadConcern.Majority,
            ReadPreference = ReadPreference.Primary,
            WriteConcern = WriteConcern.WMajority
        };

        IMongoCollection<OutboxRecord>? outboxCollection = client().GetDatabase(databaseName)
            .GetCollection<OutboxRecord>(collectionNamingConvention(typeof(OutboxRecord)), collectionSettings);
        if (outboxCollection == null)
        {
            throw new ArgumentNullException(
                $"Collection {outboxCollection} should be created in the database : {databaseName}");
        }

        var memberMapCache = new MemberMapCache();

        context.Services.AddSingleton<ISagaPersister>(new SagaPersister(versionElementName, memberMapCache));
    }
}