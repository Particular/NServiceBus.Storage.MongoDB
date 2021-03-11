namespace NServiceBus.Storage.MongoDB.Tests
{
    using System.Threading.Tasks;
    using NUnit.Framework;

    public abstract class OutboxPersisterTests
    {
        [OneTimeSetUp]
        public virtual async Task OneTimeSetUp()
        {
            configuration = new OutboxTestsConfiguration();
            await configuration.Configure();
        }

        [OneTimeTearDown]
        public virtual async Task OneTimeTearDown()
        {
            await configuration.Cleanup();
        }

        protected OutboxTestsConfiguration configuration;
    }
}