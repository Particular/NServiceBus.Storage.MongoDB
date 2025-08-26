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
    public static void TimeToKeepOutboxDeduplicationData(this OutboxSettings outboxSettings, TimeSpan timeToKeepOutboxDeduplicationData)
    {
        ArgumentNullException.ThrowIfNull(outboxSettings);

        outboxSettings.GetSettings().GetOrCreate<OutboxPersistenceConfiguration>().TimeToKeepDeduplicationData = timeToKeepOutboxDeduplicationData;
    }
}