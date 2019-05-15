namespace NServiceBus.Storage.MongoDB.Tests
{
    using System;
    using System.Configuration;

    class ConnectionStringProvider
    {
        public static string GetConnectionString()
        {
            var containerConnectionString = Environment.GetEnvironmentVariable("ContainerUrl");
            if (string.IsNullOrWhiteSpace(containerConnectionString) == false)
                return containerConnectionString;

            return ConfigurationManager.ConnectionStrings["MongoDB"].ConnectionString;
        }
    }
}