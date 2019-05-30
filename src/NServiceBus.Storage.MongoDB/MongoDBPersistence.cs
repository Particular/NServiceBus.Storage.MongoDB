namespace NServiceBus
{
    using System;
    using System.Text.RegularExpressions;
    using MongoDB.Driver;
    using Features;
    using Persistence;
    using NServiceBus.Storage.MongoDB;

    public class MongoDBPersistence : PersistenceDefinition
    {
        static IMongoClient defaultClient;

        public MongoDBPersistence()
        {
            Defaults(s =>
            {
                s.EnableFeatureByDefault<SynchronizedStorageFeature>();

                s.SetDefault(SettingsKeys.Client, (Func<IMongoClient>)(() => {
                    if (defaultClient == null)
                    {
                        defaultClient = new MongoClient();
                    }
                    return defaultClient;
                }));

                s.SetDefault(SettingsKeys.DatabaseName, SanitizeEndpointName(s.EndpointName()));
            });

            Supports<StorageType.Sagas>(s => s.EnableFeatureByDefault<SagaStorage>());
        }

        string SanitizeEndpointName(string endpointName) => Regex.Replace(endpointName, Regex.Escape(@"/\. ""$*<>:|?"), "_");
    }
}