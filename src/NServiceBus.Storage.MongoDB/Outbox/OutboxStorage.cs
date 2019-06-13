using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NServiceBus.Features;

namespace NServiceBus.Storage.MongoDB
{
    class OutboxStorage : Feature
    {
        protected override void Setup(FeatureConfigurationContext context)
        {
            throw new NotImplementedException();
        }
    }
}
