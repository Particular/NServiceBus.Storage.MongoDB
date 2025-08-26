namespace NServiceBus.Storage.MongoDB;

using global::MongoDB.Bson.Serialization.Attributes;

record struct OutboxRecordId
{
    [BsonElement("pk")] public required string PartitionKey { get; set; }
    [BsonElement("mid")] public required string MessageId { get; set; }
}