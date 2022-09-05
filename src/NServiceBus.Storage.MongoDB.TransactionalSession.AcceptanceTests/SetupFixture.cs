namespace NServiceBus.TransactionalSession.AcceptanceTests
{
    using System;
    using System.Threading.Tasks;
    using MongoDB.Driver;
    using NUnit.Framework;

    [SetUpFixture]
    public class SetupFixture
    {
        public const string DatabaseName = "TransactionalSessionAcceptanceTests";

        public static MongoClient MongoClient { get; private set; }

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            var containerConnectionString = Environment.GetEnvironmentVariable("NServiceBusStorageMongoDB_ConnectionString");

            MongoClient = string.IsNullOrWhiteSpace(containerConnectionString) ? new MongoClient() : new MongoClient(containerConnectionString);
        }

        [OneTimeTearDown]
        public async Task Cleanup()
        {
            try
            {
                await MongoClient.DropDatabaseAsync(DatabaseName);
            }
            catch (Exception e)
            {
                TestContext.WriteLine($"Error during MongoDB test cleanup: {e}");
            }
        }
    }
}