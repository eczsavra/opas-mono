using FluentAssertions;
using Opas.Domain.Primitives;
using Xunit;

namespace Opas.Domain.Tests;

public class DomainEventTests
{
    private record TestDomainEvent : DomainEvent
    {
        public string Message { get; init; } = string.Empty;
    }

    [Fact]
    public void Constructor_Should_Set_OccurredOnUtc()
    {
        var domainEvent = new TestDomainEvent { Message = "test" };
        
        domainEvent.OccurredOnUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void DomainEvent_Should_Implement_IDomainEvent()
    {
        var domainEvent = new TestDomainEvent();
        
        domainEvent.Should().BeAssignableTo<IDomainEvent>();
    }

    [Fact]
    public void Record_Equality_Should_Work_With_Components()
    {
        var event1 = new TestDomainEvent { Message = "test" };
        var event2 = new TestDomainEvent { Message = "test" };
        var event3 = new TestDomainEvent { Message = "different" };
        
        (event1 == event2).Should().BeTrue();
        (event1 == event3).Should().BeFalse();
        event1.Equals(event2).Should().BeTrue();
        event1.Equals(event3).Should().BeFalse();
    }

    [Fact]
    public void ToString_Should_Return_Readable_Representation()
    {
        var domainEvent = new TestDomainEvent { Message = "test message" };
        
        var result = domainEvent.ToString();
        
        result.Should().Contain("TestDomainEvent");
        result.Should().Contain("test message");
    }
}
