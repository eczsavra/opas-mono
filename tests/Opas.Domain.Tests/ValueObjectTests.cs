using FluentAssertions;
using Opas.Domain.Primitives;
using Xunit;

namespace Opas.Domain.Tests;

public class ValueObjectTests
{
    private class TestValueObject : ValueObject
    {
        public string Name { get; }
        public int Value { get; }

        public TestValueObject(string name, int value)
        {
            Name = name;
            Value = value;
        }

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return Name;
            yield return Value;
        }
    }

    [Fact]
    public void Equals_Should_Return_True_For_Same_Components()
    {
        var obj1 = new TestValueObject("test", 123);
        var obj2 = new TestValueObject("test", 123);
        
        (obj1 == obj2).Should().BeTrue();
        obj1.Equals(obj2).Should().BeTrue();
    }

    [Fact]
    public void Equals_Should_Return_False_For_Different_Components()
    {
        var obj1 = new TestValueObject("test", 123);
        var obj2 = new TestValueObject("different", 123);
        var obj3 = new TestValueObject("test", 456);
        
        (obj1 == obj2).Should().BeFalse();
        (obj1 == obj3).Should().BeFalse();
        obj1.Equals(obj2).Should().BeFalse();
        obj1.Equals(obj3).Should().BeFalse();
    }

    [Fact]
    public void Equals_Should_Return_False_For_Null()
    {
        var obj = new TestValueObject("test", 123);
        
        (obj == null).Should().BeFalse();
        obj.Equals((object?)null).Should().BeFalse();
    }

    [Fact]
    public void Equals_Should_Return_False_For_Different_Type()
    {
        var obj = new TestValueObject("test", 123);
        var differentObj = "not a value object";
        
        obj.Equals(differentObj).Should().BeFalse();
    }

    [Fact]
    public void GetHashCode_Should_Be_Same_For_Equal_Objects()
    {
        var obj1 = new TestValueObject("test", 123);
        var obj2 = new TestValueObject("test", 123);
        
        obj1.GetHashCode().Should().Be(obj2.GetHashCode());
    }

    [Fact]
    public void GetHashCode_Should_Be_Different_For_Different_Objects()
    {
        var obj1 = new TestValueObject("test", 123);
        var obj2 = new TestValueObject("different", 123);
        
        obj1.GetHashCode().Should().NotBe(obj2.GetHashCode());
    }

    [Fact]
    public void NotEqual_Operator_Should_Work_Correctly()
    {
        var obj1 = new TestValueObject("test", 123);
        var obj2 = new TestValueObject("different", 123);
        var obj3 = new TestValueObject("test", 123);
        
        (obj1 != obj2).Should().BeTrue();
        (obj1 != obj3).Should().BeFalse();
    }
}
