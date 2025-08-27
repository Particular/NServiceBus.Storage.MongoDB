namespace NServiceBus.Storage.MongoDB;

using System;

class OutboxRecord
{
    public required OutboxRecordId Id { get; set; }

    public DateTime? Dispatched { get; set; }

    public StorageTransportOperation[]? TransportOperations { get; set; }
}