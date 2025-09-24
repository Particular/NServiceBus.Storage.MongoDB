namespace NServiceBus.Storage.MongoDB;

using System;
using System.Collections.Generic;
using System.Linq;
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
            if (!BsonClassMap.IsClassMapRegistered(sagaMetadata.SagaEntityType))
            {
                RegisterSagaBsonClassMap(sagaMetadata);

                usesDefaultClassMap = true;
            }

            sagaEntityToClassMapDiagnostics.Add(new(sagaMetadata.SagaEntityType.FullName!, usesDefaultClassMap));

            if (sagaMetadata.TryGetCorrelationProperty(out var property) && property.Name != "Id")
            {
                _ = memberMapCache.GetOrAdd(sagaMetadata.SagaEntityType, property);
            }
        }
        return sagaEntityToClassMapDiagnostics;
    }

    static void RegisterSagaBsonClassMap(SagaMetadata sagaMetadata)
    {
        var genericClassMapType = typeof(BsonClassMap<>).MakeGenericType(sagaMetadata.SagaEntityType);
        var classMap = Activator.CreateInstance(genericClassMapType) as BsonClassMap;
        classMap.AutoMap();
        classMap.SetIgnoreExtraElements(true);
        var tryRegisterClassMapNonGeneric = typeof(BsonClassMap).GetMethods()
            .Where(x => x is { Name: "TryRegisterClassMap", IsGenericMethodDefinition: true })
            .Single(m =>
                m.GetParameters().FirstOrDefault()?.ParameterType.ToString() == typeof(BsonClassMap<>).ToString());
        var tryRegisterClassMapGeneric = tryRegisterClassMapNonGeneric!.MakeGenericMethod(sagaMetadata.SagaEntityType);
        tryRegisterClassMapGeneric.Invoke(null, [classMap]);
    }
}