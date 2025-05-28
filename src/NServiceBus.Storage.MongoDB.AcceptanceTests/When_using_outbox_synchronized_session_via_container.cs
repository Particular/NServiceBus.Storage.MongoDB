namespace NServiceBus.AcceptanceTests;

using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Storage.MongoDB;

[TestFixture]
public partial class When_using_outbox_synchronized_session_via_container : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_inject_synchronized_session_into_handler()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<Endpoint>(b => b.When(s => s.SendLocal(new MyMessage())))
            .Done(c => c.Done)
            .Run()
            .ConfigureAwait(false);

        Assert.That(context.RepositoryHasMongoSession, Is.True);
        AssertPartitionPart(context);
    }

    partial void AssertPartitionPart(Context scenarioContext);

    public class Context : ScenarioContext
    {
        public bool Done { get; set; }
        public bool RepositoryHasMongoSession { get; set; }
    }

    public class Endpoint : EndpointConfigurationBuilder
    {
        public Endpoint()
        {
            EndpointSetup<DefaultServer>(config =>
            {
                config.ConfigureTransport().TransportTransactionMode = TransportTransactionMode.ReceiveOnly;

                config.EnableOutbox();
                config.RegisterComponents(c =>
                {
                    c.AddScoped<MyRepository>();
                });
            });
        }

        public class MyMessageHandler : IHandleMessages<MyMessage>
        {
            public MyMessageHandler(MyRepository repository, Context context)
            {
                this.context = context;
                this.repository = repository;
            }


            public Task Handle(MyMessage message, IMessageHandlerContext handlerContext)
            {
                repository.DoSomething();
                context.Done = true;
                return Task.CompletedTask;
            }

            Context context;
            MyRepository repository;
        }
    }

    public class MyRepository
    {
        public MyRepository(IMongoSynchronizedStorageSession storageSession, Context context)
        {
            this.storageSession = storageSession;
            this.context = context;
        }

        public void DoSomething() => context.RepositoryHasMongoSession = storageSession.MongoSession != null;

        IMongoSynchronizedStorageSession storageSession;
        Context context;
    }

    public class MyMessage : IMessage
    {
        public string Property { get; set; }
    }
}