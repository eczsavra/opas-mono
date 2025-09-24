using FluentAssertions;
using Opas.Domain.Entities;
using Xunit;

namespace Opas.Domain.Tests;

public class ProductTests
{
    [Fact]
    public void Create_Should_Succeed_With_Minimum_Fields()
    {
        var p = Product.Create("8690000000001", "Paracetamol");

        p.Gtin.Should().Be("8690000000001");
        p.Name.Should().Be("Paracetamol");
        p.IsActive.Should().BeTrue();
        p.Form.Should().BeNull();
        p.Strength.Should().BeNull();
        p.Manufacturer.Should().BeNull();
    }

    [Theory]
    [InlineData(null, "Paracetamol")]
    [InlineData("", "Paracetamol")]
    [InlineData("   ", "Paracetamol")]
    [InlineData("869", "Paracetamol")]          // çok kısa
    [InlineData("123456789012345678901", "X")]  // 21 hane (20 üstü)
    public void Create_Should_Throw_On_Invalid_Gtin(string? gtin, string name)
    {
        Action act = () => Product.Create(gtin!, name);
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("8690000000001", null)]
    [InlineData("8690000000001", "")]
    [InlineData("8690000000001", "   ")]
    public void Create_Should_Throw_On_Empty_Name(string gtin, string? name)
    {
        Action act = () => Product.Create(gtin, name!);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Rename_And_UpdateDetails_Should_Update_And_Touch_Audit()
    {
        var p = Product.Create("8690000000001", "Para");
        var before = p.UpdatedAtUtc;

        p.Rename("Paracetamol");
        p.UpdateDetails("Tablet", "500 mg", "OPAS Ref");

        p.Name.Should().Be("Paracetamol");
        p.Form.Should().Be("Tablet");
        p.Strength.Should().Be("500 mg");
        p.Manufacturer.Should().Be("OPAS Ref");
        p.UpdatedAtUtc.Should().NotBe(before);
    }
}
