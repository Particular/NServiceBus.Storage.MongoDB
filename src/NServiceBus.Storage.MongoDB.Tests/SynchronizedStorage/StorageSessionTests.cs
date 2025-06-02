namespace NServiceBus.Storage.MongoDB.Tests;

using System;
using Extensibility;
using NUnit.Framework;

[TestFixture]
public class StorageSessionTests
{
    [Test]
    public void Should_allow_multiple_calls_to_dispose()
    {
        using var sessionHandle = ClientProvider.Client.StartSession();
        var session = new StorageSession(sessionHandle, "db-name", new ContextBag(), t => t.FullName, true,
            TimeSpan.Zero);

        Assert.DoesNotThrow(() => session.Dispose());
        Assert.DoesNotThrow(() => session.Dispose());
    }
}