using Opas.Shared.Stock;

namespace Opas.Infrastructure.Services;

/// <summary>
/// Main orchestrator service for stock import operations
/// </summary>
public class StockImportService
{
    private readonly FileParserService _fileParser;
    private readonly ColumnDetectionService _columnDetector;
    private readonly ProductMatchingService _productMatcher;

    public StockImportService(
        FileParserService fileParser,
        ColumnDetectionService columnDetector,
        ProductMatchingService productMatcher)
    {
        _fileParser = fileParser;
        _columnDetector = columnDetector;
        _productMatcher = productMatcher;
    }

    /// <summary>
    /// Analyze an imported file and detect products
    /// </summary>
    public async Task<AnalyzeImportFileResponse> AnalyzeFileAsync(
        AnalyzeImportFileRequest request,
        string tenantId,
        string connectionString)
    {
        // Step 1: Parse file
        var parsedData = await _fileParser.ParseFileAsync(
            request.FileName,
            request.FileType,
            request.FileContent);

        // Step 2: Detect columns
        var detectedColumns = _columnDetector.DetectColumns(parsedData.Headers);

        // Step 3: Match products for each row
        var importRows = new List<ImportRow>();
        int matchedCount = 0;
        int unmatchedCount = 0;

        for (int i = 0; i < parsedData.Rows.Count; i++)
        {
            var rowData = parsedData.Rows[i];
            var errors = new List<string>();
            var warnings = new List<string>();

            // Try to match product
            ProductMatch? match = null;
            try
            {
                match = await _productMatcher.MatchProductAsync(
                    rowData,
                    detectedColumns,
                    tenantId,
                    connectionString);

                if (match != null)
                {
                    matchedCount++;
                    
                    if (match.MatchType == "fuzzy" && match.MatchScore < 90)
                    {
                        warnings.Add($"Belirsiz eşleşme (Skor: {match.MatchScore}%). Kontrol edin.");
                    }
                }
                else
                {
                    unmatchedCount++;
                    warnings.Add("Ürün bulunamadı. Manuel eşleştirme gerekli.");
                }
            }
            catch (Exception ex)
            {
                unmatchedCount++;
                errors.Add($"Eşleştirme hatası: {ex.Message}");
            }

            importRows.Add(new ImportRow
            {
                RowNumber = i + 2, // +2 because header is row 1, data starts at row 2
                RawData = rowData,
                Match = match,
                Errors = errors.Count > 0 ? errors : null,
                Warnings = warnings.Count > 0 ? warnings : null
            });
        }

        // Step 4: Generate warnings
        var globalWarnings = new List<string>();
        
        if (unmatchedCount > 0)
        {
            globalWarnings.Add($"{unmatchedCount} ürün eşleştirilemedi.");
        }

        var lowConfidenceColumns = detectedColumns.Where(c => c.ConfidenceScore < 80).ToList();
        if (lowConfidenceColumns.Count > 0)
        {
            globalWarnings.Add($"{lowConfidenceColumns.Count} kolon düşük güvenle tespit edildi. Kontrol edin.");
        }

        return new AnalyzeImportFileResponse
        {
            DetectedColumns = detectedColumns,
            Rows = importRows,
            TotalRows = parsedData.Rows.Count,
            MatchedRows = matchedCount,
            UnmatchedRows = unmatchedCount,
            Warnings = globalWarnings.Count > 0 ? globalWarnings : null
        };
    }
}

