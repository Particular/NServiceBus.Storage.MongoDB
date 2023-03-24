namespace NServiceBus.Storage.MongoDB.AcceptanceTests
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using global::MongoDB.Bson.Serialization;
    using NServiceBus.AcceptanceTests;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NUnit.Framework;

    [TestFixture]
    public class When_using_custom_class_map : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_use_defined_map()
        {
            var runId = Guid.NewGuid();

            var context = await Scenario.Define<Context>(c => c.RunId = runId)
                .WithEndpoint<EndpointWithCustomMapping>(b => b.When((session, ctx) => session.SendLocal(new StartSaga
                {
                    RunId = ctx.RunId
                })))
                .Done(c => c.Done)
                .Run();

            Assert.AreEqual("SetByMapping", context.SomeValuePassedIntoTheConstructor);
        }

        public class Context : ScenarioContext
        {
            public Guid RunId { get; set; }
            public bool Done { get; set; }
            public string SomeValuePassedIntoTheConstructor { get; set; }
        }

        public class EndpointWithCustomMapping : EndpointConfigurationBuilder
        {
            public EndpointWithCustomMapping()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    BsonClassMap
                        .RegisterClassMap<SagaWithCustomMap.SagaWithCustomMapSagaData>(
                            m =>
                            {
                                m.AutoMap();
                                m.SetIgnoreExtraElements(true);

                                m.MapProperty(s => s.SomeValueSetByMapping).SetDefaultValue("SetByMapping");
                            });
                });
            }

            public class SagaWithCustomMap : Saga<SagaWithCustomMap.SagaWithCustomMapSagaData>,
                IAmStartedByMessages<StartSaga>,
                IHandleMessages<CompleteSaga>
            {
                public SagaWithCustomMap(Context context) => testContext = context;

                public Task Handle(StartSaga message, IMessageHandlerContext context)
                {
                    Data.RunId = message.RunId;
                    return context.SendLocal(new CompleteSaga { RunId = message.RunId });
                }

                public Task Handle(CompleteSaga message, IMessageHandlerContext context)
                {
                    MarkAsComplete();
                    testContext.SomeValuePassedIntoTheConstructor = Data.SomeValueSetByMapping;
                    testContext.Done = true;
                    return Task.CompletedTask;
                }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaWithCustomMapSagaData> mapper) =>
                    mapper.MapSaga(s => s.RunId)
                        .ToMessage<StartSaga>(m => m.RunId)
                        .ToMessage<CompleteSaga>(m => m.RunId);

                public class SagaWithCustomMapSagaData : ContainSagaData
                {
                    public Guid RunId { get; set; }
                    public string SomeValueSetByMapping { get; set; }
                }

                readonly Context testContext;
            }
        }

        public class StartSaga : ICommand
        {
            public Guid RunId { get; set; }
        }

        public class CompleteSaga : ICommand
        {
            public Guid RunId { get; set; }
        }
    }
}