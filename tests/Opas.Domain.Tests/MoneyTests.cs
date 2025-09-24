using FluentAssertions;
using Opas.Domain.ValueObjects;
using Xunit;

namespace Opas.Domain.Tests;

public class MoneyTests
{
    [Theory]
    [InlineData(0, "TRY", 0.00, "TRY")]
    [InlineData(12.3, "try", 12.30, "TRY")]
    [InlineData(19.999, "USD", 20.00, "USD")] // yuvarlama
    public void TryCreate_Should_Succeed(decimal amount, string currency, decimal expected, string expectedCur)
    {
        var ok = Money.TryCreate(amount, currency, out var money, out var err);

        ok.Should().BeTrue();
        money!.Amount.Should().Be(expected);
        money.Currency.Should().Be(expectedCur);
        money.ToString().Should().Be($"{expected:0.00} {expectedCur}");
        err.Should().BeNull();
    }

    [Theory]
    [InlineData(-1, "TRY")]
    [InlineData(10, null)]
    [InlineData(10, "")]
    [InlineData(10, "TR")]
    [InlineData(10, "TRYX")]
    public void TryCreate_Should_Fail(decimal amount, string? currency)
    {
        var ok = Money.TryCreate(amount, currency, out var money, out var err);

        ok.Should().BeFalse();
        money.Should().BeNull();
        err.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Equality_Should_Use_Amount_And_Currency()
    {
        Money.TryCreate(12.30m, "TRY", out var m1, out _).Should().BeTrue();
        Money.TryCreate(12.3m,  "try", out var m2, out _).Should().BeTrue();

        (m1 == m2).Should().BeTrue();
        m1!.GetHashCode().Should().Be(m2!.GetHashCode());
    }
}
