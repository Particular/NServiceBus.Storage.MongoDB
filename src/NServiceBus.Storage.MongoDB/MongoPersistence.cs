﻿using System;
using MongoDB.Driver;
using NServiceBus.Features;
using NServiceBus.Persistence;
using NServiceBus.Storage.MongoDB;
using NServiceBus.Storage.MongoDB.Subscriptions;

namespace NServiceBus
{
    /// <summary>
    /// </summary>
    public class MongoPersistence : PersistenceDefinition
    {
        /// <summary>
        /// </summary>
        public MongoPersistence()
        {
            Defaults(s =>
            {
                s.EnableFeatureByDefault<SynchronizedStorage>();

                s.SetDefault(SettingsKeys.MongoClient, (Func<IMongoClient>)(() =>
                {
                    if (defaultClient == null)
                    {
                        defaultClient = new MongoClient();
                    }

                    return defaultClient;
                }));

                s.SetDefault(SettingsKeys.DatabaseName, s.EndpointName());

                s.SetDefault(SettingsKeys.CollectionNamingConvention, (Func<Type, string>)(type => type.Name.ToLower()));

                s.SetDefault(DefaultDatabaseSettings);
            });

            Supports<StorageType.Sagas>(s => s.EnableFeatureByDefault<SagaStorage>());
            Supports<StorageType.Outbox>(s => s.EnableFeatureByDefault<OutboxStorage>());
            Supports<StorageType.Subscriptions>(s => s.EnableFeatureByDefault<SubscriptionStorage>());
        }

        static IMongoClient defaultClient;

        internal static MongoDatabaseSettings DefaultDatabaseSettings { get; } = new MongoDatabaseSettings
        {
            ReadConcern = ReadConcern.Majority,
            WriteConcern = WriteConcern.WMajority,
            ReadPreference = ReadPreference.Primary
        };
    }
}