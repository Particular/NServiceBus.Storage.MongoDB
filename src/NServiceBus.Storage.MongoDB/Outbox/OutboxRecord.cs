namespace NServiceBus.Storage.MongoDB;

using System;
using global::MongoDB.Bson.Serialization.Attributes;

class OutboxRecord
{
    [BsonId]
    public required OutboxRecordId Id { get; set; }

    public DateTime? Dispatched { get; set; }

    public StorageTransportOperation[]? TransportOperations { get; set; }
}