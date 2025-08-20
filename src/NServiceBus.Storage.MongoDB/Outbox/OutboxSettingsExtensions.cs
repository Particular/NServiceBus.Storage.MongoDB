namespace NServiceBus.Storage.MongoDB;

using System;
using Settings;

static class OutboxSettingsExtensions
{
    public static TimeSpan GetTimeToKeepOutboxDeduplicationData(this IReadOnlySettings settings)
    {
        if (!settings.TryGet(SettingsKeys.TimeToKeepOutboxDeduplicationData, out TimeSpan timeToKeepOutboxDeduplicationData))
        {
            timeToKeepOutboxDeduplicationData = DefaultTimeToKeepOutboxDeduplicationData;
        }

        return timeToKeepOutboxDeduplicationData;
    }

    static readonly TimeSpan DefaultTimeToKeepOutboxDeduplicationData = TimeSpan.FromDays(7);
}