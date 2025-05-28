namespace NServiceBus.Storage.MongoDB.Tests;

using System;
using System.Threading.Tasks;
using NUnit.Framework;
using Sagas;

public class When_persisting_a_saga_entity : SagaPersisterTests
{
    [SetUp]
    public async Task Setup()
    {
        entity = new PropertyTypesTestSagaData
        {
            Id = Guid.NewGuid(),
            TestComponent = new TestComponent { Property = "Prop" },
            DateTimeProperty = DateTime.Parse("12/02/2010 12:00:00.01").ToUniversalTime(),
            Status = Statuses.AnotherStatus,
            PolymorphicRelatedProperty = new PolymorphicProperty { SomeInt = 9 },
            RelatedClass = new RelatedClass { Id = Guid.NewGuid() }
        };
        relatedClass = entity.RelatedClass;

        var insertContextBag = configuration.GetContextBagForSagaStorage();
        using (var insertSession = configuration.SessionFactory())
        {
            await insertSession.Open(insertContextBag);
            var correlationProperty = new SagaCorrelationProperty(nameof(IContainSagaData.Id), entity.Id);

            await configuration.SagaStorage.Save(entity, correlationProperty, insertSession, insertContextBag);
            await insertSession.CompleteAsync();
        }

        savedEntity = await GetById<PropertyTypesTestSagaData>(entity.Id).ConfigureAwait(false);
    }

    [Test]
    public void Public_setters_and_getters_of_concrete_classes_should_be_persisted()
    {
        Assert.That(savedEntity.TestComponent, Is.EqualTo(entity.TestComponent));
    }

    [Test]
    public void Datetime_properties_should_be_persisted()
    {
        Assert.That(savedEntity.DateTimeProperty, Is.EqualTo(entity.DateTimeProperty));
    }

    [Test]
    public void Enums_should_be_persisted()
    {
        Assert.That(savedEntity.Status, Is.EqualTo(entity.Status));
    }

    [Test]
    public void Inherited_property_classes_should_be_persisted()
    {
        Assert.That(savedEntity.PolymorphicRelatedProperty, Is.EqualTo(entity.PolymorphicRelatedProperty));
    }

    [Test]
    public void Related_entities_should_be_persisted()
    {
        Assert.That(savedEntity.RelatedClass.Id, Is.EqualTo(relatedClass.Id));
    }

    PropertyTypesTestSagaData entity;
    PropertyTypesTestSagaData savedEntity;
    RelatedClass relatedClass;
}