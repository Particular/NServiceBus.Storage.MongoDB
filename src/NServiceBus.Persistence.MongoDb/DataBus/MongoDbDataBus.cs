namespace NServiceBus.Persistence.MongoDB.DataBus
{
    using System;
    using NServiceBus.DataBus;

    public class MongoDbDataBus : DataBusDefinition
    {
        protected override Type ProvidedByFeature()
        {
            return typeof (MongoDbDataBusPersistence);
        }
    }
}