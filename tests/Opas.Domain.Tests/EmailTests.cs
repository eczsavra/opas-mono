using FluentAssertions;
using Opas.Domain.ValueObjects;
using Xunit;

namespace Opas.Domain.Tests;

public class EmailTests
{
    [Theory]
    [InlineData("admin@opaseos.com")]
    [InlineData("USER@Example.org")]
    public void TryCreate_Should_Succeed_For_Valid_Emails(string input)
    {
        var ok = Email.TryCreate(input, out var email, out var err);

        ok.Should().BeTrue();
        email!.Value.Should().Be(input.Trim());
        err.Should().BeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("invalid")]
    [InlineData("foo@bar")]
    [InlineData("foo@bar.")]
    public void TryCreate_Should_Fail_For_Invalid_Emails(string? input)
    {
        var ok = Email.TryCreate(input, out var email, out var err);

        ok.Should().BeFalse();
        email.Should().BeNull();
        err.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Equality_Should_Ignore_Case()
    {
        Email.TryCreate("Admin@Opas.com", out var e1, out _).Should().BeTrue();
        Email.TryCreate("admin@opas.com", out var e2, out _).Should().BeTrue();

        (e1 == e2).Should().BeTrue();
        e1!.GetHashCode().Should().Be(e2!.GetHashCode());
    }
}
