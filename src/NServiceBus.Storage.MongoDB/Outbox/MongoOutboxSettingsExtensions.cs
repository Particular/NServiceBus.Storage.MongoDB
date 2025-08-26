namespace NServiceBus;

using System;
using Configuration.AdvancedExtensibility;
using Outbox;
using Storage.MongoDB;

/// <summary>
/// Outbox settings extensions for MongoDB persistence.
/// </summary>
public static class MongoOutboxSettingsExtensions
{
    /// <summary>
    /// Configures the amount of time to keep outbox deduplication data.
    /// </summary>
    public static OutboxSettings TimeToKeepOutboxDeduplicationData(this OutboxSettings outboxSettings, TimeSpan timeToKeepOutboxDeduplicationData)
    {
        ArgumentNullException.ThrowIfNull(outboxSettings);

        outboxSettings.GetSettings().GetOrCreate<OutboxPersistenceConfiguration>().TimeToKeepDeduplicationData = timeToKeepOutboxDeduplicationData;

        return outboxSettings;
    }

    /// <summary>
    /// When retrieving outbox messages, the persister tries to load the outbox records assuming the new or the old
    /// outbox record schema. For collections that are known to only contain the new schema this fallback can be disabled.
    /// </summary>
    public static OutboxSettings DisableReadFallback(this OutboxSettings outboxSettings)
    {
        ArgumentNullException.ThrowIfNull(outboxSettings);

        outboxSettings.GetSettings().GetOrCreate<OutboxPersistenceConfiguration>().ReadFallbackEnabled = false;

        return outboxSettings;
    }
}