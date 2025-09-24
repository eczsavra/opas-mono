namespace Opas.Shared;

public static class Guard
{
    public static void NotNull(object? input, string name)
    {
        if (input is null) throw new ArgumentNullException(name);
    }

    public static void NotNullOrWhiteSpace(string? input, string name)
    {
        if (string.IsNullOrWhiteSpace(input))
            throw new ArgumentException($"{name} cannot be empty.", name);
    }
}
