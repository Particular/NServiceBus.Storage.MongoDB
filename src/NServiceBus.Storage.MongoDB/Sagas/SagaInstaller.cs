namespace NServiceBus.Storage.MongoDB

using System;
using System.Threading;
using System.Threading.Tasks;
using global::MongoDB.Bson;
using global::MongoDB.Bson.Serialization;
using global::MongoDB.Driver;
using NServiceBus.Installation;
using NServiceBus.Sagas;
using NServiceBus.Settings;

class SagaInstaller(IReadOnlySettings settings) : INeedToInstallSomething
{
	public Task Install(string identity, CancellationToken cancellationToken = default)
	{
		if (!settings.TryGet(SettingsKeys.VersionElementName, out string versionElementName))
		{
			versionElementName = SagaPersister.DefaultVersionElementName;
		}

		if (!settings.TryGet<Func<IMongoClient>>(SettingsKeys.MongoClient, out var client))
		{
			return Task.CompletedTask;
		}
		var databaseName = settings.Get<string>(SettingsKeys.DatabaseName);
		var collectionNamingConvention = settings.Get<Func<Type, string>>(SettingsKeys.CollectionNamingConvention);
		var sagaMetadataCollection = settings.Get<SagaMetadataCollection>();

		InitializeSagaDataTypes(client(), databaseName, collectionNamingConvention, sagaMetadataCollection);
		return Task.CompletedTask;
	}

	internal static void InitializeSagaDataTypes(IMongoClient client, string databaseName, Func<Type, string> collectionNamingConvention, SagaMetadataCollection sagaMetadataCollection)
	{
		var databaseSettings = new MongoDatabaseSettings
		{
			ReadConcern = ReadConcern.Majority,
			ReadPreference = ReadPreference.Primary,
			WriteConcern = WriteConcern.WMajority
		};
		var database = client.GetDatabase(databaseName, databaseSettings);

		foreach (var sagaMetadata in sagaMetadataCollection)
		{
			if (!BsonClassMap.IsClassMapRegistered(sagaMetadata.SagaEntityType))
			{
				var classMap = new BsonClassMap(sagaMetadata.SagaEntityType);
				classMap.AutoMap();
				classMap.SetIgnoreExtraElements(true);

				BsonClassMap.RegisterClassMap(classMap);
			}

			var collectionName = collectionNamingConvention(sagaMetadata.SagaEntityType);

			if (sagaMetadata.TryGetCorrelationProperty(out var property) && property.Name != "Id")
			{
				var propertyElementName = sagaMetadata.SagaEntityType.GetElementName(property.Name);

				var indexModel = new CreateIndexModel<BsonDocument>(new BsonDocumentIndexKeysDefinition<BsonDocument>(new BsonDocument(propertyElementName, 1)), new CreateIndexOptions
				{ Unique = true });
				database.GetCollection<BsonDocument>(collectionName).Indexes.CreateOne(indexModel);
			}
			else
			{
				try
				{
					database.CreateCollection(collectionName);
				}
				catch (MongoCommandException ex) when (ex.Code == 48 && ex.CodeName == "NamespaceExists")
				{
					//Collection already exists, so swallow the exception
				}
			}
		}
	}
}

