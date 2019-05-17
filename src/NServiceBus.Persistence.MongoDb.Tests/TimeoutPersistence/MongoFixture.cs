namespace NServiceBus.Persistence.MongoDb.Tests.TimeoutPersistence
{
    using System;
    using System.Globalization;
    using global::MongoDB.Driver;
    using NServiceBus.Persistence.MongoDB.Timeout;
    using NUnit.Framework;

    public class MongoFixture
    {
        private TimeoutPersister _storage;
        private IMongoDatabase _database;
        private MongoClient _client;
        private readonly string _databaseName = "Test_" + DateTime.Now.Ticks.ToString(CultureInfo.InvariantCulture);

        [SetUp]
        public void SetupContext()
        {
            var connectionString = ConnectionStringProvider.GetConnectionString();

            _client = new MongoClient(connectionString);
            _database = _client.GetDatabase(_databaseName);

            _storage = new TimeoutPersister("MyTestEndpoint", _database);
        }

        protected TimeoutPersister Storage => _storage;

        [TearDown]
        public void TeardownContext() => _client.DropDatabase(_databaseName);
    }
}