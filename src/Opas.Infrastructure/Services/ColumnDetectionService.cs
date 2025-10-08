using Opas.Shared.Stock;
using System.Text.RegularExpressions;

namespace Opas.Infrastructure.Services;

/// <summary>
/// Service for intelligently detecting column types from imported files
/// </summary>
public class ColumnDetectionService
{
    // Pattern definitions for column detection
    private static readonly Dictionary<string, List<ColumnPattern>> FieldPatterns = new()
    {
        ["product_name"] = new()
        {
            new("ürün", 100, true),
            new("urun", 100, true),
            new("ilaç", 100, true),
            new("ilac", 100, true),
            new("drug", 90, true),
            new("product", 90, true),
            new("name", 70, true),
            new("ad", 70, true),
            new("isim", 70, true),
            new("tanım", 70, true),
            new("tanim", 70, true),
            new("description", 60, true)
        },
        ["gtin"] = new()
        {
            new("gtin", 100, false),
            new("barcode", 95, true),
            new("barkod", 95, true),
            new("kod", 80, true),
            new("code", 80, true),
            new("ean", 90, false)
        },
        ["quantity"] = new()
        {
            new("miktar", 100, true),
            new("adet", 100, true),
            new("quantity", 100, true),
            new("qty", 95, true),
            new("amount", 85, true),
            new("stok", 80, true),
            new("stock", 80, true)
        },
        ["unit_cost"] = new()
        {
            new("maliyet", 100, true),
            new("cost", 100, true),
            new("fiyat", 90, true),
            new("price", 90, true),
            new("birim fiyat", 95, true),
            new("birim_fiyat", 95, true),
            new("unit price", 95, true),
            new("unit_price", 95, true)
        },
        ["serial_number"] = new()
        {
            new("seri", 100, true),
            new("serial", 100, true),
            new("seri no", 100, true),
            new("serial no", 100, true),
            new("sn", 80, false)
        },
        ["expiry_date"] = new()
        {
            new("skt", 100, false),
            new("son kullanma", 100, true),
            new("expiry", 100, true),
            new("expire", 95, true),
            new("tarih", 70, true),
            new("date", 70, true)
        },
        ["bonus_quantity"] = new()
        {
            new("mal fazlası", 100, true),
            new("mal_fazlası", 100, true),
            new("mf", 100, false),
            new("bonus", 95, true),
            new("bedava", 90, true),
            new("free", 90, true)
        }
    };

    /// <summary>
    /// Detect column types from headers
    /// </summary>
    public List<DetectedColumn> DetectColumns(List<string> headers)
    {
        var result = new List<DetectedColumn>();

        for (int i = 0; i < headers.Count; i++)
        {
            var header = headers[i];
            var detection = DetectColumnType(header, i);
            result.Add(detection);
        }

        return result;
    }

    private DetectedColumn DetectColumnType(string header, int columnIndex)
    {
        var normalizedHeader = NormalizeText(header);
        
        string? bestField = null;
        int bestScore = 0;

        foreach (var (field, patterns) in FieldPatterns)
        {
            foreach (var pattern in patterns)
            {
                var score = CalculateMatchScore(normalizedHeader, pattern);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestField = field;
                }
            }
        }

        return new DetectedColumn
        {
            OriginalHeader = header,
            DetectedField = bestField ?? "unknown",
            ColumnIndex = columnIndex,
            ConfidenceScore = bestScore
        };
    }

    private int CalculateMatchScore(string normalizedHeader, ColumnPattern pattern)
    {
        var normalizedPattern = NormalizeText(pattern.Pattern);

        // Exact match
        if (normalizedHeader == normalizedPattern)
        {
            return pattern.BaseScore;
        }

        // Contains match
        if (pattern.PartialMatch && normalizedHeader.Contains(normalizedPattern))
        {
            return pattern.BaseScore - 10;
        }

        // Fuzzy match (Levenshtein-like simple check)
        if (pattern.PartialMatch && AreSimilar(normalizedHeader, normalizedPattern))
        {
            return pattern.BaseScore - 20;
        }

        return 0;
    }

    private string NormalizeText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        // Remove Turkish characters
        text = text.Replace('ı', 'i')
                   .Replace('İ', 'i')
                   .Replace('ğ', 'g')
                   .Replace('Ğ', 'g')
                   .Replace('ü', 'u')
                   .Replace('Ü', 'u')
                   .Replace('ş', 's')
                   .Replace('Ş', 's')
                   .Replace('ö', 'o')
                   .Replace('Ö', 'o')
                   .Replace('ç', 'c')
                   .Replace('Ç', 'c');

        // Remove special characters and normalize
        text = Regex.Replace(text, @"[^a-zA-Z0-9]", " ");
        text = Regex.Replace(text, @"\s+", " ");
        text = text.Trim().ToLowerInvariant();

        return text;
    }

    private bool AreSimilar(string s1, string s2)
    {
        if (string.IsNullOrEmpty(s1) || string.IsNullOrEmpty(s2))
            return false;

        // Simple similarity check: check if one contains most of the other
        var shorter = s1.Length < s2.Length ? s1 : s2;
        var longer = s1.Length < s2.Length ? s2 : s1;

        if (longer.Contains(shorter))
            return true;

        // Check word overlap
        var words1 = s1.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var words2 = s2.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        var commonWords = words1.Intersect(words2).Count();
        var totalWords = Math.Max(words1.Length, words2.Length);

        return totalWords > 0 && (double)commonWords / totalWords > 0.5;
    }
}

/// <summary>
/// Pattern definition for column detection
/// </summary>
public record ColumnPattern(string Pattern, int BaseScore, bool PartialMatch);

