namespace NServiceBus.AcceptanceTests
{
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;
    using Storage.MongoDB;

    [TestFixture]
    public class When_using_synchronized_session_via_container : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_inject_synchronized_session_into_handler()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<Endpoint>(b => b.When(s => s.SendLocal(new MyMessage())))
                .Done(c => c.Done)
                .Run()
                .ConfigureAwait(false);

            Assert.That(context.HandlerHasMongoSession, Is.True);
        }

        public class Context : ScenarioContext
        {
            public bool Done { get; set; }
            public bool HandlerHasMongoSession { get; set; }
        }

        public class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint() => EndpointSetup<DefaultServer>();

            public class MyHandler : IHandleMessages<MyMessage>
            {
                public MyHandler(IMongoSynchronizedStorageSession session, Context context)
                {
                    this.session = session;
                    this.context = context;
                }

                public Task Handle(MyMessage message, IMessageHandlerContext handlerContext)
                {
                    context.Done = true;
                    context.HandlerHasMongoSession = session.MongoSession != null;

                    return Task.CompletedTask;
                }

                Context context;
                IMongoSynchronizedStorageSession session;
            }
        }

        public class MyMessage : IMessage
        {
            public string Property { get; set; }
        }
    }
}