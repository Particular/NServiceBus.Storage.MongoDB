using NUnit.Framework;
using Particular.Approvals;
using PublicApiGenerator;

namespace NServiceBus.Storage.MongoDB.Tests
{
    [TestFixture]
    public class APIApprovals
    {
        [Test]
        public void Approve()
        {
            var publicApi = ApiGenerator.GeneratePublicApi(typeof(MongoDBPersistence).Assembly, excludeAttributes: new[] { "System.Runtime.Versioning.TargetFrameworkAttribute" });
            Approver.Verify(publicApi);
        }
    }
}
