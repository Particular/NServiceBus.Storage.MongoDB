namespace NServiceBus.Storage.MongoDB;

using global::MongoDB.Bson;
using global::MongoDB.Bson.Serialization;
using global::MongoDB.Bson.Serialization.Serializers;

/// <summary>
/// This serializer makes sure that we can read both the legacy and new format of the OutboxRecordId.
/// The legacy format was just a string (the MessageId) while the new format is a document with PartitionKey and MessageId.
/// When writing, we always use the new format.
/// </summary>
/// <remarks>This serializer can be removed once we no longer support fallback reads.</remarks>
sealed class OutboxRecordIdSerializer : SerializerBase<OutboxRecordId>
{
    // Cache the default serializer to avoid looking it up on every serialization/deserialization.
    // The default serializer handles the new format (document with PartitionKey and MessageId) based on the class mapping
    // this should not lead to recursive calls to the OutboxRecordIdSerializer.
    static readonly IBsonSerializer<OutboxRecordId> DefaultSerializer =
        BsonSerializer.LookupSerializer<OutboxRecordId>();

    public override OutboxRecordId Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        var type = context.Reader.GetCurrentBsonType();
        if (type == BsonType.String)
        {
            var messageId = context.Reader.ReadString();
            return new OutboxRecordId { PartitionKey = null, MessageId = messageId };
        }

        return DefaultSerializer.Deserialize(context, args);
    }

    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, OutboxRecordId value)
        => DefaultSerializer.Serialize(context, args, value);
}