namespace NServiceBus.Storage.MongoDB;

using Microsoft.Extensions.DependencyInjection;
using NServiceBus.Features;

sealed class InstallerFeature : Feature
{
    public InstallerFeature()
    {
        Defaults(s => s.SetDefault(new InstallerSettings()));
        DependsOn<Features.SynchronizedStorage>();
    }

    protected override void Setup(FeatureConfigurationContext context)
    {
        InstallerSettings settings = context.Settings.Get<InstallerSettings>();
        context.Services.AddSingleton(settings);
    }
}