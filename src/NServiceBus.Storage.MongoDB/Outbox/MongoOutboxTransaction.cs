using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NServiceBus.Outbox;

namespace NServiceBus.Storage.MongoDB.Outbox
{
    class MongoOutboxTransaction : OutboxTransaction
    {
        public Task Commit()
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
