namespace NServiceBus.Storage.MongoDB;

using System;

sealed class OutboxPersistenceConfiguration
{
    public TimeSpan TimeToKeepDeduplicationData
    {
        get => timeToKeepDeduplicationData;
        set
        {
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(value, TimeSpan.Zero);

            var seconds = Math.Ceiling(value.TotalSeconds);
            timeToKeepDeduplicationData = TimeSpan.FromSeconds(seconds);
        }
    }

    public bool ReadFallbackEnabled { get; set; } = true;

    public string PartitionKey { get; set; } = null!; // will be set by defaults

    TimeSpan timeToKeepDeduplicationData = TimeSpan.FromDays(7);
}