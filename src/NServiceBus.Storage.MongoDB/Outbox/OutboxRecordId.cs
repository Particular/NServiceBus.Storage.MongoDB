namespace NServiceBus.Storage.MongoDB;

struct OutboxRecordId
{
    public required string? PartitionKey { get; set; }
    public required string MessageId { get; set; }
}