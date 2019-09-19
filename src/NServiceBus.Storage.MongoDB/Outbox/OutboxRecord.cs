namespace NServiceBus.Storage.MongoDB
{
    using System;
    using Outbox;

    class OutboxRecord
    {
        public string Id { get; set; }

        public DateTime? Dispatched { get; set; }

        public TransportOperation[] TransportOperations { get; set; }
    }
}