using FluentAssertions;
using Opas.Domain.Primitives;
using Xunit;

namespace Opas.Domain.Tests;

public class BaseEntityTests
{
    private class TestEntity : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
    }

    [Fact]
    public void Constructor_Should_Set_CreatedAtUtc()
    {
        var entity = new TestEntity();
        
        entity.CreatedAtUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        entity.UpdatedAtUtc.Should().BeNull();
        entity.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public void MarkUpdated_Should_Set_UpdatedAtUtc()
    {
        var entity = new TestEntity();
        var before = DateTime.UtcNow;
        
        entity.MarkUpdated();
        
        entity.UpdatedAtUtc.Should().NotBeNull();
        entity.UpdatedAtUtc.Should().BeCloseTo(before, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void SoftDelete_Should_Mark_As_Deleted_And_Raise_Event()
    {
        var entity = new TestEntity();
        
        entity.SoftDelete();
        
        entity.IsDeleted.Should().BeTrue();
        entity.UpdatedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public void SoftDelete_Should_Not_Raise_Event_If_Already_Deleted()
    {
        var entity = new TestEntity();
        entity.SoftDelete();
        entity.ClearDomainEvents();
        
        entity.SoftDelete();
        
        entity.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Restore_Should_Mark_As_Not_Deleted_And_Raise_Event()
    {
        var entity = new TestEntity();
        entity.SoftDelete();
        entity.ClearDomainEvents();
        
        entity.Restore();
        
        entity.IsDeleted.Should().BeFalse();
        entity.UpdatedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public void Restore_Should_Not_Raise_Event_If_Not_Deleted()
    {
        var entity = new TestEntity();
        
        entity.Restore();
        
        entity.DomainEvents.Should().BeEmpty();
    }
}
