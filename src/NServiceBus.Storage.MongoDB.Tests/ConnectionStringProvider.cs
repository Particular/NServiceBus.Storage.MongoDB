namespace NServiceBus.Storage.MongoDB.Tests
{
    using System;

    class ConnectionStringProvider
    {
        public static string GetConnectionString()
        {
            var containerConnectionString = Environment.GetEnvironmentVariable("ContainerUrl");

            if (string.IsNullOrWhiteSpace(containerConnectionString) == false)
            {
                return containerConnectionString;
            }

            return string.Empty; //TODO fix this up for real
        }
    }
}