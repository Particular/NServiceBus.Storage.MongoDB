namespace NServiceBus.AcceptanceTests;

using System.Collections.Concurrent;
using System.Threading.Tasks;
using AcceptanceTesting;
using Configuration.AdvancedExtensibility;
using EndpointTemplates;
using NUnit.Framework;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Storage.MongoDB;

[TestFixture]
public class When_using_synchronized_session_via_container_and_storage_session_extension : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_commit_all_operations_using_the_same_batch()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<Endpoint>(b =>
            {
                b.CustomConfig((cfg, ctx) =>
                {
                    ctx.InterceptedCommands = cfg.GetSettings()
                        .Get<ConcurrentQueue<string>>(ConfigureEndpointMongoPersistence.InterceptedCommands);
                });
                b.When(s => s.SendLocal(new MyMessage()));
            })
            .Done(c => c.FirstHandlerIsDone && c.SecondHandlerIsDone)
            .Run()
            .ConfigureAwait(false);

        Assert.That(context.InterceptedCommands, Has.One.Items.Match("COMMITTRANSACTION"));
    }

    public class Context : ScenarioContext
    {
        public bool FirstHandlerIsDone { get; set; }
        public bool SecondHandlerIsDone { get; set; }
        public ConcurrentQueue<string> InterceptedCommands { get; set; }
    }

    public class Endpoint : EndpointConfigurationBuilder
    {
        public Endpoint() => EndpointSetup<DefaultServer>();

        public class MyHandlerUsingStorageSession : IHandleMessages<MyMessage>
        {
            public MyHandlerUsingStorageSession(IMongoSynchronizedStorageSession session, Context context)
            {
                this.session = session;
                this.context = context;
            }

            public async Task Handle(MyMessage message, IMessageHandlerContext handlerContext)
            {
                var entity = new MyEntity { Id = ObjectId.GenerateNewId(), Data = "MyCustomData" };
                var collection = session.MongoSession.Client.GetDatabase(ConfigureEndpointMongoPersistence.DatabaseName)
                    .GetCollection<MyEntity>("myentity");
                await collection.InsertOneAsync(session.MongoSession, entity);
                context.FirstHandlerIsDone = true;
            }

            Context context;
            IMongoSynchronizedStorageSession session;
        }

        public class MyHandlerUsingExtensionMethod : IHandleMessages<MyMessage>
        {
            public MyHandlerUsingExtensionMethod(Context context)
            {
                this.context = context;
            }

            public async Task Handle(MyMessage message, IMessageHandlerContext handlerContext)
            {
                var session = handlerContext.SynchronizedStorageSession.MongoPersistenceSession();

                var entity = new MyEntity { Id = ObjectId.GenerateNewId(), Data = "MyCustomData" };
                var collection = session.MongoSession.Client.GetDatabase(ConfigureEndpointMongoPersistence.DatabaseName)
                    .GetCollection<MyEntity>("myentity");
                await collection.InsertOneAsync(session.MongoSession, entity);
                context.SecondHandlerIsDone = true;
            }

            Context context;
        }
    }

    public class MyEntity
    {
        [BsonId] public ObjectId Id { get; set; }
        public string Data { get; set; }
    }

    public class MyMessage : IMessage
    {
        public string Property { get; set; }
    }
}