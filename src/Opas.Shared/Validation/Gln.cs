namespace Opas.Shared.Validation;

public static class Gln
{
    /// <summary>
    /// GS1 GLN: 13 hane, son hane mod10 check digit
    /// </summary>
    public static bool IsValid(string? gln)
    {
        if (string.IsNullOrWhiteSpace(gln)) return false;
        gln = gln.Trim();
        if (gln.Length != 13) return false;
        if (!gln.All(char.IsDigit)) return false;

        int sum = 0;
        // Soldan sağa 12 haneyi tart (1..12), 1-index: tek=1x, çift=3x
        for (int i = 0; i < 12; i++)
        {
            int digit = gln[i] - '0';
            sum += ((i + 1) % 2 == 1) ? digit : digit * 3;
        }
        int check = (10 - (sum % 10)) % 10;
        int last = gln[12] - '0';
        return check == last;
    }
}
