namespace Opas.Api.Control;

public sealed class GlnUpsertDto
{
    public string Gln { get; set; } = default!;
    public string? CompanyName { get; set; }
    public string? City { get; set; }
    public string? Town { get; set; }
    public string Source { get; set; } = "manual";
}
