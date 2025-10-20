namespace NServiceBus;

using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using Persistence;
using Storage.MongoDB;

/// <summary>
/// Used to configure NServiceBus to use MongoDB persistence.
/// </summary>
public partial class MongoPersistence : PersistenceDefinition, IPersistenceDefinitionFactory<MongoPersistence>
{
    MongoPersistence(object? _)
    {
        Defaults(s =>
        {
            // Deferred to feature default activation to give users a chance to register their own mappings before defaults apply.
            // This ensures user configs take precedence, despite being slightly counter to the idea of feature defaults.
            // Can't do much earlier due to static nature of class mappings and serialization extensions.
            SafeRegisterDefaultGuidSerializer();

            s.SetDefault<IMongoClientProvider>(new DefaultMongoClientProvider());

            s.SetDefault(SettingsKeys.DatabaseName, s.EndpointName());

            s.SetDefault(SettingsKeys.CollectionNamingConvention, DefaultCollectionNamingConvention);

            s.SetDefault(DefaultDatabaseSettings);
            s.SetDefault(DefaultCollectionSettings);

            s.SetDefault(new MemberMapCache());

            s.SetDefault(new InstallerSettings());
        });

        Supports<StorageType.Sagas, SagaStorage>();
        Supports<StorageType.Outbox, OutboxStorage>();
        Supports<StorageType.Subscriptions, SubscriptionStorage>();
    }

    internal static void SafeRegisterDefaultGuidSerializer()
    {
        try
        {
            // By default, we are using the Standard representation for Guids which is the Guid representation
            // that should be used moving forward as of version 2.19 of the MongoDB .NET Driver
            // https://www.mongodb.com/docs/drivers/csharp/v2.19/fundamentals/serialization/guid-serialization/
            _ = BsonSerializer.TryRegisterSerializer(GuidSerializer.StandardInstance);
        }
        catch (BsonSerializationException ex)
        {
            // TryRegisterSerializer can throw if a serializer for Guid is already registered,
            // but the serializer is a different instance than the one we are trying to register
            // In that case we assume that the serializer is already registered correctly. If it is not,
            // then the unspecified format with binary representation will be used which leads to runtime
            // exceptions when reading/writing Guids
            //
            // Looking up the serializer automatically caches the serializer, so we can only do this in the catch block
            if (BsonSerializer.LookupSerializer<Guid>() is GuidSerializer
                {
                    GuidRepresentation: GuidRepresentation.Unspecified, Representation: BsonType.Binary
                })
            {
                throw new Exception(
                    "A GuidSerializer using the Unspecified representation is already registered which" +
                    " indicates the default serializer has already been used. Register the GuidSerializer" +
                    " with the preferred representation before using the mongodb client as early as possible.", ex);
            }
        }
    }

    static MongoPersistence IPersistenceDefinitionFactory<MongoPersistence>.Create() => new(null);

    internal static MongoDatabaseSettings DefaultDatabaseSettings { get; } = new()
    {
        ReadConcern = ReadConcern.Majority,
        WriteConcern = WriteConcern.WMajority,
        ReadPreference = ReadPreference.Primary
    };

    internal static MongoCollectionSettings DefaultCollectionSettings { get; } = new()
    {
        ReadConcern = ReadConcern.Majority,
        WriteConcern = WriteConcern.WMajority,
        ReadPreference = ReadPreference.Primary
    };

    internal static readonly TimeSpan DefaultTransactionTimeout = TimeSpan.FromSeconds(60);
    internal static readonly Func<Type, string> DefaultCollectionNamingConvention = type => type.Name.ToLower();
}