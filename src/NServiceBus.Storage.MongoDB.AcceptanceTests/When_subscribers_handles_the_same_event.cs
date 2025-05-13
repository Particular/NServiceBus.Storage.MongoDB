namespace NServiceBus.AcceptanceTests.Outbox;

using System;
using System.Threading;
using System.Threading.Tasks;
using AcceptanceTesting;
using AcceptanceTesting.Customization;
using EndpointTemplates;
using Features;
using NServiceBus.Pipeline;
using NUnit.Framework;

public class When_subscribers_handles_the_same_event : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_be_processed_by_all_subscribers()
    {
        Requires.OutboxPersistence();

        Context context = await Scenario.Define<Context>()
            .WithEndpoint<Publisher>(b =>
                b.When(session => session.SendLocal(new TriggerMessage()))
            )
            .WithEndpoint<Subscriber1>()
            .WithEndpoint<Subscriber2>()
            .Done(c => c.Subscriber1GotTheEvent && c.Subscriber2GotTheEvent)
            .Run(TimeSpan.FromSeconds(10));

        Assert.Multiple(() =>
        {
            Assert.That(context.Subscriber1GotTheEvent, Is.True);
            Assert.That(context.Subscriber2GotTheEvent, Is.True);
        });
    }

    public class Context : ScenarioContext
    {
        public bool Subscriber1GotTheEvent { get; set; }
        public bool Subscriber2GotTheEvent { get; set; }
    }

    public class Publisher : EndpointConfigurationBuilder
    {
        public Publisher() => EndpointSetup<DefaultPublisher>();

        public class TriggerHandler : IHandleMessages<TriggerMessage>
        {
            public Task Handle(TriggerMessage message, IMessageHandlerContext context)
                => context.Publish(new MyEvent());
        }
    }

    public class Subscriber1 : EndpointConfigurationBuilder
    {
        public Subscriber1() =>
            EndpointSetup<DefaultServer>(c =>
            {
                c.ConfigureTransport().TransportTransactionMode = TransportTransactionMode.ReceiveOnly;
                c.EnableOutbox();
            });

        public class MyHandler(Context testContext) : IHandleMessages<MyEvent>
        {
            public Task Handle(MyEvent message, IMessageHandlerContext context)
            {
                testContext.Subscriber1GotTheEvent = true;
                return Task.CompletedTask;
            }
        }
    }

    public class Subscriber2 : EndpointConfigurationBuilder
    {
        public Subscriber2() =>
            EndpointSetup<DefaultServer>(c =>
            {
                c.ConfigureTransport().TransportTransactionMode = TransportTransactionMode.ReceiveOnly;
                c.EnableOutbox();
            });

        public class MyHandler(Context testContext) : IHandleMessages<MyEvent>
        {
            public Task Handle(MyEvent messageThatIsEnlisted, IMessageHandlerContext context)
            {
                testContext.Subscriber2GotTheEvent = true;
                return Task.CompletedTask;
            }
        }
    }

    public class MyEvent : IEvent
    {
    }

    public class TriggerMessage : ICommand
    {
    }
}