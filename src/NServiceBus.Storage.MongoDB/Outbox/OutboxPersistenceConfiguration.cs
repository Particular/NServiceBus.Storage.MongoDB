namespace NServiceBus.Storage.MongoDB.Outbox;

using System;

sealed class OutboxPersistenceConfiguration
{
    public TimeSpan TimeToKeepDeduplicationData
    {
        get => field;
        set
        {
            var seconds = Math.Ceiling(value.TotalSeconds);
            field = TimeSpan.FromSeconds(seconds);
        }
    }
}