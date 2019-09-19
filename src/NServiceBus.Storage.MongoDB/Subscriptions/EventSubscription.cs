namespace NServiceBus.Storage.MongoDB
{
    class EventSubscription
    {
        public string MessageTypeName { get; set; }

        public string TransportAddress { get; set; }

        public string Endpoint { get; set; }
    }
}