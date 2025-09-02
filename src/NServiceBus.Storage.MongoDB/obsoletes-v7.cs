#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
namespace NServiceBus;

using System;
using Particular.Obsoletes;

public static partial class MongoSettingsExtensions
{
    [ObsoleteMetadata(
        ReplacementTypeOrMember = "MongoOutboxSettingsExtensions.TimeToKeepOutboxDeduplicationData",
        RemoveInVersion = "8",
        TreatAsErrorFromVersion = "7")]
    [Obsolete("Use 'MongoOutboxSettingsExtensions.TimeToKeepOutboxDeduplicationData' instead. Will be removed in version 8.0.0.", true)]
    public static PersistenceExtensions<MongoPersistence> TimeToKeepOutboxDeduplicationData(
        this PersistenceExtensions<MongoPersistence> persistenceExtensions, TimeSpan timeToKeepOutboxDeduplicationData) =>
        throw new NotImplementedException();
}
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member