namespace NServiceBus.Storage.MongoDB;

using Features;
using global::MongoDB.Bson.Serialization;
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

        context.Services.AddSingleton<ISagaPersister>(new SagaPersister(versionElementName, new MemberMapCache()));

        var sagaMetadataCollection = context.Settings.Get<SagaMetadataCollection>();
        RegisterSagaEntityClassMappings(sagaMetadataCollection);
    }

    internal static void RegisterSagaEntityClassMappings(SagaMetadataCollection sagaMetadataCollection)
    {
        foreach (var sagaMetadata in sagaMetadataCollection)
        {
            if (BsonClassMap.IsClassMapRegistered(sagaMetadata.SagaEntityType))
            {
                continue;
            }

            var classMap = new BsonClassMap(sagaMetadata.SagaEntityType);
            classMap.AutoMap();
            classMap.SetIgnoreExtraElements(true);

            BsonClassMap.RegisterClassMap(classMap);
        }
    }
}