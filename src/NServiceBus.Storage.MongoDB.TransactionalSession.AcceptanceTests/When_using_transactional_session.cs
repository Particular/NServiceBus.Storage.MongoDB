namespace NServiceBus.TransactionalSession.AcceptanceTests
{
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using Microsoft.Extensions.DependencyInjection;
    using MongoDB.Driver;
    using NUnit.Framework;
    using Storage.MongoDB;

    public class When_using_transactional_session : NServiceBusAcceptanceTest
    {
        const string CollectionName = "SampleDocumentCollection";

        [TestCase(true)]
        [TestCase(false)]
        public async Task Should_send_messages_and_store_document_in_synchronized_session_on_transactional_session_commit(bool outboxEnabled)
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<AnEndpoint>(s => s.When(async (_, ctx) =>
                {
                    using var scope = ctx.ServiceProvider.CreateScope();
                    using var transactionalSession = scope.ServiceProvider.GetRequiredService<ITransactionalSession>();
                    await transactionalSession.Open();
                    ctx.SessionId = transactionalSession.SessionId;

                    var mongoSession = transactionalSession.SynchronizedStorageSession.GetClientSession();
                    await mongoSession.Client.GetDatabase(SetupFixture.DatabaseName)
                        .GetCollection<SampleDocument>(CollectionName)
                        .InsertOneAsync(mongoSession, new SampleDocument { Id = transactionalSession.SessionId });

                    await transactionalSession.SendLocal(new SampleMessage());

                    await transactionalSession.Commit().ConfigureAwait(false);
                }))
                .Done(c => c.MessageReceived)
                .Run();

            var documents = await SetupFixture.MongoClient.GetDatabase(SetupFixture.DatabaseName)
                .GetCollection<SampleDocument>(CollectionName)
                .FindAsync<SampleDocument>(Builders<SampleDocument>.Filter.Where(d => d.Id == context.SessionId));
            Assert.That(documents.ToList().Count, Is.EqualTo(1));
        }

        [TestCase(true)]
        [TestCase(false)]
        public async Task Should_send_messages_and_store_document_in_mongo_session_on_transactional_session_commit(bool outboxEnabled)
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<AnEndpoint>(s => s.When(async (_, ctx) =>
                {
                    using var scope = ctx.ServiceProvider.CreateScope();
                    using var transactionalSession = scope.ServiceProvider.GetRequiredService<ITransactionalSession>();
                    await transactionalSession.Open();
                    ctx.SessionId = transactionalSession.SessionId;

                    var mongoSession = scope.ServiceProvider.GetRequiredService<IMongoSynchronizedStorageSession>();
                    await mongoSession.MongoSession.Client.GetDatabase(SetupFixture.DatabaseName)
                        .GetCollection<SampleDocument>(CollectionName)
                        .InsertOneAsync(mongoSession.MongoSession, new SampleDocument { Id = transactionalSession.SessionId });

                    await transactionalSession.SendLocal(new SampleMessage());

                    await transactionalSession.Commit();
                }))
                .Done(c => c.MessageReceived)
                .Run();

            var documents = await SetupFixture.MongoClient.GetDatabase(SetupFixture.DatabaseName)
                .GetCollection<SampleDocument>(CollectionName)
                .FindAsync<SampleDocument>(Builders<SampleDocument>.Filter.Where(d => d.Id == context.SessionId));
            Assert.That(documents.ToList().Count, Is.EqualTo(1));
        }

        [TestCase(true)]
        [TestCase(false)]
        public async Task Should_not_send_messages_and_store_document_if_session_is_not_committed(bool outboxEnabled)
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<AnEndpoint>(s => s.When(async (statelessSession, ctx) =>
                {
                    using (var scope = ctx.ServiceProvider.CreateScope())
                    using (var transactionalSession = scope.ServiceProvider.GetRequiredService<ITransactionalSession>())
                    {
                        await transactionalSession.Open(new MongoOpenSessionOptions());
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

            Assert.That(context.CompleteMessageReceived, Is.True);
            Assert.That(context.MessageReceived, Is.False);

            var documents = await SetupFixture.MongoClient.GetDatabase(SetupFixture.DatabaseName)
                .GetCollection<SampleDocument>(CollectionName)
                .FindAsync<SampleDocument>(Builders<SampleDocument>.Filter.Where(d => d.Id == context.SessionId));
            Assert.That(documents.Any(), Is.False);
        }

        [TestCase(true)]
        [TestCase(false)]
        public async Task Should_send_immediate_dispatch_messages_even_if_session_is_not_committed(bool outboxEnabled)
        {
            var result = await Scenario.Define<Context>()
                .WithEndpoint<AnEndpoint>(s => s.When(async (_, ctx) =>
                {
                    using var scope = ctx.ServiceProvider.CreateScope();
                    using var transactionalSession = scope.ServiceProvider.GetRequiredService<ITransactionalSession>();

                    await transactionalSession.Open(new MongoOpenSessionOptions());

                    var sendOptions = new SendOptions();
                    sendOptions.RequireImmediateDispatch();
                    sendOptions.RouteToThisEndpoint();
                    await transactionalSession.Send(new SampleMessage(), sendOptions);
                }))
                .Done(c => c.MessageReceived)
                .Run();

            Assert.That(result.MessageReceived, Is.True);
        }

        [TestCase(true)]
        [TestCase(false)]
        public async Task Should_allow_using_synchronized_storage_even_when_there_are_no_outgoing_operations(bool outboxEnabled)
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<AnEndpoint>(s => s.When(async (statelessSession, ctx) =>
                {
                    using (var scope = ctx.ServiceProvider.CreateScope())
                    using (var transactionalSession = scope.ServiceProvider.GetRequiredService<ITransactionalSession>())
                    {
                        await transactionalSession.Open();
                        ctx.SessionId = transactionalSession.SessionId;

                        var mongoSession = transactionalSession.SynchronizedStorageSession.GetClientSession();
                        await mongoSession.Client.GetDatabase(SetupFixture.DatabaseName)
                            .GetCollection<SampleDocument>(CollectionName)
                            .InsertOneAsync(mongoSession, new SampleDocument { Id = transactionalSession.SessionId });

                        // Deliberately not sending any messages via the transactional session before committing
                        await transactionalSession.Commit();
                    }

                    //Send immediately dispatched message to finish the test
                    await statelessSession.SendLocal(new CompleteTestMessage());
                }))
                .Done(c => c.CompleteMessageReceived)
                .Run();

            var documents = await SetupFixture.MongoClient.GetDatabase(SetupFixture.DatabaseName)
                .GetCollection<SampleDocument>(CollectionName)
                .FindAsync<SampleDocument>(Builders<SampleDocument>.Filter.Where(d => d.Id == context.SessionId));
            Assert.That(documents.ToList().Count, Is.EqualTo(1));
        }

        class Context : TransactionalSessionTestContext
        {
            public bool MessageReceived { get; set; }
            public bool CompleteMessageReceived { get; set; }
            public string SessionId { get; set; }
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

            class SampleHandler(Context testContext) : IHandleMessages<SampleMessage>
            {
                public Task Handle(SampleMessage message, IMessageHandlerContext context)
                {
                    testContext.MessageReceived = true;

                    return Task.CompletedTask;
                }
            }

            class CompleteTestMessageHandler(Context testContext) : IHandleMessages<CompleteTestMessage>
            {
                public Task Handle(CompleteTestMessage message, IMessageHandlerContext context)
                {
                    testContext.CompleteMessageReceived = true;

                    return Task.CompletedTask;
                }
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