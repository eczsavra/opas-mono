using FuzzySharp;
using Npgsql;
using Opas.Shared.Stock;

namespace Opas.Infrastructure.Services;

/// <summary>
/// Service for matching imported products to database products
/// </summary>
public class ProductMatchingService
{
    private const int FuzzyMatchThreshold = 75;
    private const int MaxAlternatives = 3;

    /// <summary>
    /// Match a product from imported data to database products
    /// </summary>
    public async Task<ProductMatch?> MatchProductAsync(
        Dictionary<string, string?> rowData,
        List<DetectedColumn> columns,
        string tenantId,
        string connectionString)
    {
        // Extract relevant data from row
        var productName = GetColumnValue(rowData, columns, "product_name");
        var gtin = GetColumnValue(rowData, columns, "gtin");

        if (string.IsNullOrWhiteSpace(productName) && string.IsNullOrWhiteSpace(gtin))
        {
            return null;
        }

        // Try exact GTIN match first (best case)
        if (!string.IsNullOrWhiteSpace(gtin))
        {
            var gtinMatch = await MatchByGtinAsync(gtin, tenantId, connectionString);
            if (gtinMatch != null)
            {
                return gtinMatch;
            }
        }

        // Try fuzzy name matching
        if (!string.IsNullOrWhiteSpace(productName))
        {
            return await MatchByNameAsync(productName, tenantId, connectionString);
        }

        return null;
    }

    private async Task<ProductMatch?> MatchByGtinAsync(string gtin, string tenantId, string connectionString)
    {
        var cleanGtin = CleanGtin(gtin);

        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync();

        var query = @"
            SELECT product_id, drug_name, gtin
            FROM products
            WHERE gtin = @gtin
            LIMIT 1";

        await using var cmd = new NpgsqlCommand(query, conn);
        cmd.Parameters.AddWithValue("@gtin", cleanGtin);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new ProductMatch
            {
                ProductId = reader.GetString(0),
                ProductName = reader.GetString(1),
                Gtin = reader.IsDBNull(2) ? null : reader.GetString(2),
                MatchType = "exact_gtin",
                MatchScore = 100
            };
        }

        return null;
    }

    private async Task<ProductMatch?> MatchByNameAsync(string productName, string tenantId, string connectionString)
    {
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync();

        // Get all products for fuzzy matching
        var query = @"
            SELECT product_id, drug_name, gtin
            FROM products
            LIMIT 1000"; // Limit for performance

        var candidates = new List<(string ProductId, string ProductName, string? Gtin, int Score)>();

        await using var cmd = new NpgsqlCommand(query, conn);
        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var dbProductId = reader.GetString(0);
            var dbProductName = reader.GetString(1);
            var dbGtin = reader.IsDBNull(2) ? null : reader.GetString(2);

            // Calculate fuzzy match score
            var score = Fuzz.Ratio(productName.ToLowerInvariant(), dbProductName.ToLowerInvariant());

            if (score >= FuzzyMatchThreshold)
            {
                candidates.Add((dbProductId, dbProductName, dbGtin, score));
            }
        }

        // Sort by score descending
        candidates = candidates.OrderByDescending(c => c.Score).ToList();

        if (candidates.Count == 0)
        {
            return null;
        }

        var best = candidates[0];
        var alternatives = candidates
            .Skip(1)
            .Take(MaxAlternatives)
            .Select(c => new AlternativeMatch
            {
                ProductId = c.ProductId,
                ProductName = c.ProductName,
                Gtin = c.Gtin,
                MatchScore = c.Score
            })
            .ToList();

        return new ProductMatch
        {
            ProductId = best.ProductId,
            ProductName = best.ProductName,
            Gtin = best.Gtin,
            MatchType = best.Score == 100 ? "exact_name" : "fuzzy",
            MatchScore = best.Score,
            Alternatives = alternatives.Count > 0 ? alternatives : null
        };
    }

    private string? GetColumnValue(Dictionary<string, string?> rowData, List<DetectedColumn> columns, string fieldName)
    {
        var column = columns.FirstOrDefault(c => c.DetectedField == fieldName);
        if (column == null)
            return null;

        return rowData.TryGetValue(column.OriginalHeader, out var value) ? value : null;
    }

    private string CleanGtin(string gtin)
    {
        // Remove all non-numeric characters
        return new string(gtin.Where(char.IsDigit).ToArray());
    }
}

