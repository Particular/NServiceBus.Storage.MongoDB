#nullable disable

namespace NServiceBus.Storage.MongoDB;

using System;
using System.Collections.Generic;
using Outbox;

class StorageTransportOperation
{
    public StorageTransportOperation()
    {
    }

    public StorageTransportOperation(TransportOperation source)
    {
        MessageId = source.MessageId;
        Options = source.Options != null ? new Dictionary<string, string>(source.Options) : [];
        Body = source.Body;
        Headers = source.Headers;
    }

    public string MessageId { get; set; }
    public Dictionary<string, string> Options { get; set; }
    public ReadOnlyMemory<byte> Body { get; set; }
    public Dictionary<string, string> Headers { get; set; }


    public TransportOperation ToTransportType() =>
        new(MessageId, new Transport.DispatchProperties(Options), Body, Headers);
}