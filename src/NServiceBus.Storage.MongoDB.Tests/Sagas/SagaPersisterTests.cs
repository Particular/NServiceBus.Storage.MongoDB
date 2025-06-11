namespace NServiceBus.Storage.MongoDB.Tests;

using System;
using System.Threading.Tasks;
using NUnit.Framework;

public abstract class SagaPersisterTests
{
    [OneTimeSetUp]
    public virtual async Task OneTimeSetUp()
    {
        configuration = new SagaTestsConfiguration();
        await configuration.Configure();
    }

    [OneTimeTearDown]
    public virtual async Task OneTimeTearDown() => await configuration.Cleanup();

    protected async Task<TSagaData> GetById<TSagaData>(Guid sagaId) where TSagaData : class, IContainSagaData, new()
    {
        var readContextBag = configuration.GetContextBagForSagaStorage();
        using var readSession = configuration.SessionFactory();
        await readSession.Open(readContextBag);
        TSagaData sagaData = await configuration.SagaStorage.Get<TSagaData>(sagaId, readSession, readContextBag);

        await readSession.CompleteAsync();

        return sagaData;
    }

    protected async Task<TSagaData> GetByProperty<TSagaData>(string propertyName, object propertyValue)
        where TSagaData : class, IContainSagaData, new()
    {
        var readContextBag = configuration.GetContextBagForSagaStorage();
        using var readSession = configuration.SessionFactory();
        await readSession.Open(readContextBag);
        TSagaData sagaData =
            await configuration.SagaStorage.Get<TSagaData>(propertyName, propertyValue, readSession, readContextBag);

        await readSession.CompleteAsync();

        return sagaData;
    }

    protected SagaTestsConfiguration configuration;
}