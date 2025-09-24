using System.Text.RegularExpressions;
using Opas.Domain.Primitives;

namespace Opas.Domain.ValueObjects;

public sealed class Email : ValueObject
{
    private static readonly Regex Rx = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);

    public string Value { get; }
    private Email(string value) => Value = value;

    public static bool TryCreate(string? input, out Email? email, out string? error)
    {
        email = null; error = null;
        if (string.IsNullOrWhiteSpace(input)) { error = "email is required."; return false; }
        var v = input.Trim();
        if (v.Length > 254) { error = "email too long."; return false; }
        if (!Rx.IsMatch(v)) { error = "email format invalid."; return false; }
        email = new Email(v);
        return true;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value.ToLowerInvariant();
    }

    public override string ToString() => Value;
}
