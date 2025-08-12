namespace NServiceBus.Storage.MongoDB;

using Features;
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
    }
}