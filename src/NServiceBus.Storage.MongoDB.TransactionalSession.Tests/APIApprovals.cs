namespace NServiceBus.Storage.MongoDB.Tests;

using NUnit.Framework;
using Particular.Approvals;
using PublicApiGenerator;
using TransactionalSession;

[TestFixture]
public class APIApprovals
{
    [Test]
    public void Approve()
    {
        var publicApi = typeof(MongoOpenSessionOptions).Assembly.GeneratePublicApi(new ApiGeneratorOptions
        {
            ExcludeAttributes =
            [
                "System.Runtime.Versioning.TargetFrameworkAttribute",
                "System.Reflection.AssemblyMetadataAttribute"
            ]
        });
        Approver.Verify(publicApi);
    }
}