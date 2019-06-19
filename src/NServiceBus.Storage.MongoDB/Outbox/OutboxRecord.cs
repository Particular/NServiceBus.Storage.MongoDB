﻿using NServiceBus.Outbox;

namespace NServiceBus.Storage.MongoDB
{
    class OutboxRecord
    {
        public string Id { get; set; }

        public TransportOperation[] TransportOperations { get; set; }
    }
}
