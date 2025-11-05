namespace NServiceBus.Storage.MongoDB;

using System.Collections.Generic;
using System.Threading;
using Features;
using global::MongoDB.Bson.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Sagas;

class SagaStorage : Feature
{
    SagaStorage()
    {
        EnableByDefault<SynchronizedStorage>();
        DependsOn<Sagas>();
        DependsOn<SynchronizedStorage>();
    }

    protected override void Setup(FeatureConfigurationContext context)
    {
        if (!context.Settings.TryGet(SettingsKeys.VersionElementName, out string versionElementName))
        {
            versionElementName = SagaPersister.DefaultVersionElementName;
        }

        var memberMapCache = context.Settings.Get<MemberMapCache>();
        context.Services.AddSingleton<ISagaPersister>(new SagaPersister(versionElementName, memberMapCache));

        var sagaMetadataCollection = context.Settings.Get<SagaMetadataCollection>();
        var classMappings = RegisterSagaEntityClassMappings(sagaMetadataCollection, memberMapCache);

        context.Settings.AddStartupDiagnosticsSection("NServiceBus.Storage.MongoDB.Sagas", new
        {
            VersionElement = versionElementName,
            ClassMappings = classMappings
        });
    }

    internal readonly struct MappingMetadata(string sagaEntity, bool usesDefaultClassMap)
    {
        public string SagaEntity { get; } = sagaEntity;
        public bool UsesDefaultClassMap { get; } = usesDefaultClassMap;
    }

    internal static IReadOnlyCollection<MappingMetadata> RegisterSagaEntityClassMappings(SagaMetadataCollection sagaMetadataCollection, MemberMapCache memberMapCache)
    {
        var sagaEntityToClassMapDiagnostics = new List<MappingMetadata>();

        foreach (var sagaMetadata in sagaMetadataCollection)
        {
            var usesDefaultClassMap = false;

            lock (classMapLock)
            {
                if (!BsonClassMap.IsClassMapRegistered(sagaMetadata.SagaEntityType))
                {
                    var classMap = new BsonClassMap(sagaMetadata.SagaEntityType);
                    classMap.AutoMap();
                    classMap.SetIgnoreExtraElements(true);

                    BsonClassMap.RegisterClassMap(classMap);

                    usesDefaultClassMap = true;
                }
            }

            sagaEntityToClassMapDiagnostics.Add(new(sagaMetadata.SagaEntityType.FullName!, usesDefaultClassMap));

            if (sagaMetadata.TryGetCorrelationProperty(out var property) && property.Name != "Id")
            {
                _ = memberMapCache.GetOrAdd(sagaMetadata.SagaEntityType, property);
            }
        }

        return sagaEntityToClassMapDiagnostics;
    }

    static readonly Lock classMapLock = new();
}