using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NServiceBus.Storage.MongoDB.Tests
{
    public class PropertyTypesTestSaga : Saga<PropertyTypesTestSagaData>, IAmStartedByMessages<PropertyTypesTestSagaDataStartMessage>
    {
        public Task Handle(PropertyTypesTestSagaDataStartMessage message, IMessageHandlerContext context)
        {
            throw new NotImplementedException();
        }

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<PropertyTypesTestSagaData> mapper)
        {
            mapper.ConfigureMapping<PropertyTypesTestSagaDataStartMessage>(msg => msg.Id).ToSaga(saga => saga.Id);
        }
    }

    public class PropertyTypesTestSagaData : IContainSagaData
    {
        public virtual Guid Id { get; set; }

        public virtual string Originator { get; set; }

        public virtual string OriginalMessageId { get; set; }

        public virtual RelatedClass RelatedClass { get; set; }

        public virtual IList<OrderLine> OrderLines { get; set; }

        public virtual StatusEnum Status { get; set; }

        public virtual DateTime DateTimeProperty { get; set; }

        public virtual TestComponent TestComponent { get; set; }

        public virtual PolymorphicPropertyBase PolymorphicRelatedProperty { get; set; }

        public override bool Equals(object obj)
        {
            return this.EqualTo(obj, (x, y) => x.Id == y.Id);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }

    public class PolymorphicProperty : PolymorphicPropertyBase
    {
        public virtual int SomeInt { get; set; }

        public override bool Equals(object obj)
        {
            return this.EqualTo(obj, (x, y) => x.Id == y.Id && x.SomeInt == y.SomeInt);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }

    public class PolymorphicPropertyBase
    {
        public virtual Guid Id { get; set; }
    }

    public enum StatusEnum
    {
        SomeStatus, AnotherStatus
    }

    public class TestComponent
    {
        // ReSharper disable once MemberCanBePrivate.Global
        public string Property { get; set; }

        // ReSharper disable once MemberCanBePrivate.Global
        public string AnotherProperty { get; set; }

        public override bool Equals(object obj)
        {
            return this.EqualTo(obj, (x, y) =>
                x.Property == y.Property &&
                x.AnotherProperty == y.AnotherProperty);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }

    public class OrderLine
    {
        public virtual Guid Id { get; set; }

        public virtual Guid ProductId { get; set; }
    }

    public class RelatedClass
    {
        public virtual Guid Id { get; set; }
    }

    public class PropertyTypesTestSagaDataStartMessage : ICommand
    {
        public Guid Id { get; set; }
    }
}