using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NServiceBus.Extensibility;
using NServiceBus.Outbox;

namespace NServiceBus.Storage.MongoDB
{
    class OutboxPersister : IOutboxStorage
    {
        public Task<OutboxTransaction> BeginTransaction(ContextBag context)
        {
            throw new NotImplementedException();
        }

        public Task<OutboxMessage> Get(string messageId, ContextBag context)
        {
            throw new NotImplementedException();
        }

        public Task SetAsDispatched(string messageId, ContextBag context)
        {
            throw new NotImplementedException();
        }

        public Task Store(OutboxMessage message, OutboxTransaction transaction, ContextBag context)
        {
            throw new NotImplementedException();
        }
    }
}
