namespace NServiceBus.Storage.MongoDB;

using System;

sealed class OutboxPersistenceConfiguration
{
    public TimeSpan TimeToKeepDeduplicationData
    {
        get => field;
        set
        {
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(value, TimeSpan.Zero);

            var seconds = Math.Ceiling(value.TotalSeconds);
            field = TimeSpan.FromSeconds(seconds);
        }
    }

    public bool ReadFallbackEnabled { get; set; } = true;
}