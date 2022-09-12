namespace NServiceBus.TransactionalSession.AcceptanceTests
{
    using System.Threading;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using MongoDB.Driver;
    using NUnit.Framework;
    using ObjectBuilder;

    public class When_using_transactional_session : NServiceBusAcceptanceTest
    {
        const string CollectionName = "SampleDocumentCollection";

        [TestCase(true)]
        [TestCase(false)]
        public async Task Should_send_messages_on_transactional_session_commit(bool outboxEnabled)
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<AnEndpoint>(s => s.When(async (_, ctx) =>
                {
                    using var scope = ctx.Builder.CreateChildBuilder();
                    using var transactionalSession = scope.Build<ITransactionalSession>();
                    await transactionalSession.Open();
                    ctx.SessionId = transactionalSession.SessionId;

                    var mongoSession = transactionalSession.SynchronizedStorageSession.GetClientSession();
                    await mongoSession.Client.GetDatabase(SetupFixture.DatabaseName)
                        .GetCollection<SampleDocument>(CollectionName)
                        .InsertOneAsync(mongoSession, new SampleDocument { Id = transactionalSession.SessionId });

                    await transactionalSession.SendLocal(new SampleMessage(), CancellationToken.None);

                    await transactionalSession.Commit(CancellationToken.None).ConfigureAwait(false);
                }))
                .Done(c => c.MessageReceived)
                .Run();

            var documents = await SetupFixture.MongoClient.GetDatabase(SetupFixture.DatabaseName)
                .GetCollection<SampleDocument>(CollectionName)
                .FindAsync<SampleDocument>(Builders<SampleDocument>.Filter.Where(d => d.Id == context.SessionId));
            Assert.AreEqual(1, documents.ToList().Count);
        }

        [TestCase(true)]
        [TestCase(false)]
        public async Task Should_not_send_messages_if_session_is_not_committed(bool outboxEnabled)
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<AnEndpoint>(s => s.When(async (statelessSession, ctx) =>
                {
                    using (var scope = ctx.Builder.CreateChildBuilder())
                    using (var transactionalSession = scope.Build<ITransactionalSession>())
                    {
                        await transactionalSession.Open(new MongoSessionOptions());
                        ctx.SessionId = transactionalSession.SessionId;

                        await transactionalSession.SendLocal(new SampleMessage());

                        var mongoSession = transactionalSession.SynchronizedStorageSession.GetClientSession();
                        await mongoSession.Client.GetDatabase(SetupFixture.DatabaseName)
                            .GetCollection<SampleDocument>(CollectionName)
                            .InsertOneAsync(mongoSession, new SampleDocument { Id = transactionalSession.SessionId });
                    }

                    //Send immediately dispatched message to finish the test
                    await statelessSession.SendLocal(new CompleteTestMessage());
                }))
                .Done(c => c.CompleteMessageReceived)
                .Run();

            Assert.True(context.CompleteMessageReceived);
            Assert.False(context.MessageReceived);

            var documents = await SetupFixture.MongoClient.GetDatabase(SetupFixture.DatabaseName)
                .GetCollection<SampleDocument>(CollectionName)
                .FindAsync<SampleDocument>(Builders<SampleDocument>.Filter.Where(d => d.Id == context.SessionId));
            Assert.IsFalse(documents.Any());
        }

        [TestCase(true)]
        [TestCase(false)]
        public async Task Should_send_immediate_dispatch_messages_even_if_session_is_not_committed(bool outboxEnabled)
        {
            var result = await Scenario.Define<Context>()
                .WithEndpoint<AnEndpoint>(s => s.When(async (_, ctx) =>
                {
                    using var scope = ctx.Builder.CreateChildBuilder();
                    using var transactionalSession = scope.Build<ITransactionalSession>();

                    await transactionalSession.Open(new MongoSessionOptions());

                    var sendOptions = new SendOptions();
                    sendOptions.RequireImmediateDispatch();
                    sendOptions.RouteToThisEndpoint();
                    await transactionalSession.Send(new SampleMessage(), sendOptions, CancellationToken.None);
                }))
                .Done(c => c.MessageReceived)
                .Run()
                ;

            Assert.True(result.MessageReceived);
        }

        class Context : ScenarioContext, IInjectBuilder
        {
            public bool MessageReceived { get; set; }
            public bool CompleteMessageReceived { get; set; }
            public string SessionId { get; set; }
            public IBuilder Builder { get; set; }
        }

        class AnEndpoint : EndpointConfigurationBuilder
        {
            public AnEndpoint()
            {
                if ((bool)TestContext.CurrentContext.Test.Arguments[0]!)
                {
                    EndpointSetup<TransactionSessionDefaultServer>();
                }
                else
                {
                    EndpointSetup<TransactionSessionWithOutboxEndpoint>();
                }
            }

            class SampleHandler : IHandleMessages<SampleMessage>
            {
                public SampleHandler(Context testContext) => this.testContext = testContext;

                public Task Handle(SampleMessage message, IMessageHandlerContext context)
                {
                    testContext.MessageReceived = true;

                    return Task.CompletedTask;
                }

                readonly Context testContext;
            }

            class CompleteTestMessageHandler : IHandleMessages<CompleteTestMessage>
            {
                public CompleteTestMessageHandler(Context context) => testContext = context;

                public Task Handle(CompleteTestMessage message, IMessageHandlerContext context)
                {
                    testContext.CompleteMessageReceived = true;

                    return Task.CompletedTask;
                }

                readonly Context testContext;
            }
        }

        class SampleMessage : ICommand
        {
        }

        class CompleteTestMessage : ICommand
        {
        }

        class SampleDocument
        {
            public string Id { get; set; }
        }
    }
}