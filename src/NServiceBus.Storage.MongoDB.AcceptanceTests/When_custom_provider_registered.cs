namespace NServiceBus.AcceptanceTests;

using System;
using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using NUnit.Framework;
using Storage.MongoDB;

public class When_custom_provider_registered : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_be_used()
    {
        Context context = await Scenario.Define<Context>()
            .WithEndpoint<EndpointWithCustomProvider>(b => b.When(session => session.SendLocal(new StartSaga1 { DataId = Guid.NewGuid() })))
            .Done(c => c.SagaReceivedMessage)
            .Run();

        Assert.That(context.ProviderWasCalled, Is.True);
    }

    public class Context : ScenarioContext
    {
        public bool SagaReceivedMessage { get; set; }
        public bool ProviderWasCalled { get; set; }
    }

    public class EndpointWithCustomProvider : EndpointConfigurationBuilder
    {
        public EndpointWithCustomProvider() =>
            EndpointSetup<DefaultServer>(config =>
            {
                config.RegisterComponents(c =>
                    c.AddSingleton<IMongoClientProvider>(b => new CustomProvider(b.GetService<Context>())));
            });

        public class JustASaga(Context testContext) : Saga<JustASagaData>, IAmStartedByMessages<StartSaga1>
        {
            public Task Handle(StartSaga1 message, IMessageHandlerContext context)
            {
                Data.DataId = message.DataId;
                testContext.SagaReceivedMessage = true;
                MarkAsComplete();
                return Task.CompletedTask;
            }

            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<JustASagaData> mapper) => mapper.ConfigureMapping<StartSaga1>(m => m.DataId).ToSaga(s => s.DataId);
        }

        public class CustomProvider(Context testContext) : IMongoClientProvider
        {
            public IMongoClient Client
            {
                get
                {
                    if (field is not null)
                    {
                        return field;
                    }

                    var containerConnectionString = Environment.GetEnvironmentVariable("NServiceBusStorageMongoDB_ConnectionString");

                    field = string.IsNullOrWhiteSpace(containerConnectionString) ? new MongoClient() : new MongoClient(containerConnectionString);
                    testContext.ProviderWasCalled = true;
                    return field;
                }
            }
        }

        public class JustASagaData : ContainSagaData
        {
            public virtual Guid DataId { get; set; }
        }
    }

    public class StartSaga1 : ICommand
    {
        public Guid DataId { get; set; }
    }
}