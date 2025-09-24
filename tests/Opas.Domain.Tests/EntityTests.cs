using FluentAssertions;
using Opas.Domain.Primitives;
using Xunit;

namespace Opas.Domain.Tests;

public class EntityTests
{
    private class TestEntity : Entity
    {
        public string Name { get; set; } = string.Empty;
        
        public void RaiseTestEvent() => Raise(new TestDomainEvent());
    }

    private class TestDomainEvent : IDomainEvent
    {
        public DateTime OccurredOnUtc { get; init; } = DateTime.UtcNow;
    }

    [Fact]
    public void Constructor_Should_Generate_New_Id()
    {
        var entity1 = new TestEntity();
        var entity2 = new TestEntity();
        
        entity1.Id.Should().NotBe(entity2.Id);
        entity1.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void Raise_Should_Add_Domain_Event()
    {
        var entity = new TestEntity();
        
        entity.RaiseTestEvent();
        
        entity.DomainEvents.Should().HaveCount(1);
        entity.DomainEvents.First().Should().BeOfType<TestDomainEvent>();
    }

    [Fact]
    public void ClearDomainEvents_Should_Remove_All_Events()
    {
        var entity = new TestEntity();
        entity.RaiseTestEvent();
        entity.RaiseTestEvent();
        
        entity.ClearDomainEvents();
        
        entity.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Equals_Should_Compare_By_Id()
    {
        var entity1 = new TestEntity();
        var entity2 = new TestEntity();
        var entity1Copy = entity1;
        
        (entity1 == entity1Copy).Should().BeTrue();
        (entity1 == entity2).Should().BeFalse();
        entity1.Equals(entity1Copy).Should().BeTrue();
        entity1.Equals(entity2).Should().BeFalse();
    }

    [Fact]
    public void GetHashCode_Should_Use_Id()
    {
        var entity1 = new TestEntity();
        var entity2 = new TestEntity();
        
        entity1.GetHashCode().Should().Be(entity1.Id.GetHashCode());
        entity1.GetHashCode().Should().NotBe(entity2.GetHashCode());
    }
}
