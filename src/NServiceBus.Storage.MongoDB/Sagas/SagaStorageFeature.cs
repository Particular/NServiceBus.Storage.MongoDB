namespace NServiceBus.Storage.MongoDB;

using System;
using Features;
using global::MongoDB.Bson;
using global::MongoDB.Bson.Serialization;
using global::MongoDB.Driver;
using Microsoft.Extensions.DependencyInjection;
using Sagas;

class SagaStorageFeature : Feature
{
	SagaStorageFeature()
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

		if (!context.Settings.TryGet<Func<IMongoClient>>(SettingsKeys.MongoClient, out var client))
		{
			return;
		}
		var databaseName = context.Settings.Get<string>(SettingsKeys.DatabaseName);
		var collectionNamingConvention = context.Settings.Get<Func<Type, string>>(SettingsKeys.CollectionNamingConvention);
        var sagaMetadataCollection = context.Settings.Get<SagaMetadataCollection>();

        var collectionSettings = new MongoCollectionSettings
		{
			ReadConcern = ReadConcern.Majority,
			ReadPreference = ReadPreference.Primary,
			WriteConcern = WriteConcern.WMajority
		};

		var outboxCollection = client().GetDatabase(databaseName).GetCollection<OutboxRecord>(collectionNamingConvention(typeof(OutboxRecord)), collectionSettings);
		if (outboxCollection == null)
		{
			throw new ArgumentNullException($"Collection {outboxCollection} should be created in the database : {databaseName}");
		}

        var memberMapCache = new MemberMapCache();

        context.Services.AddSingleton<ISagaPersister>(new SagaPersister(versionElementName, memberMapCache));
    }
}

