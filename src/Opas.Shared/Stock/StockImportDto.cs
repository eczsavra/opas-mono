namespace Opas.Shared.Stock;

/// <summary>
/// Request for uploading and analyzing a stock import file
/// </summary>
public record AnalyzeImportFileRequest
{
    public required string FileName { get; init; }
    public required string FileType { get; init; } // "excel", "csv", "pdf", "word"
    public required byte[] FileContent { get; init; }
}

/// <summary>
/// Response containing analyzed rows and detected columns
/// </summary>
public record AnalyzeImportFileResponse
{
    public required List<DetectedColumn> DetectedColumns { get; init; }
    public required List<ImportRow> Rows { get; init; }
    public required int TotalRows { get; init; }
    public required int MatchedRows { get; init; }
    public required int UnmatchedRows { get; init; }
    public List<string>? Warnings { get; init; }
}

/// <summary>
/// Detected column from the imported file
/// </summary>
public record DetectedColumn
{
    public required string OriginalHeader { get; init; }
    public required string DetectedField { get; init; } // "product_name", "gtin", "quantity", "price", etc.
    public required int ColumnIndex { get; init; }
    public required int ConfidenceScore { get; init; } // 0-100
}

/// <summary>
/// Single row from imported file with matching information
/// </summary>
public record ImportRow
{
    public required int RowNumber { get; init; }
    public required Dictionary<string, string?> RawData { get; init; }
    public ProductMatch? Match { get; init; }
    public List<string>? Errors { get; init; }
    public List<string>? Warnings { get; init; }
}

/// <summary>
/// Product matching information for an import row
/// </summary>
public record ProductMatch
{
    public required string ProductId { get; init; }
    public required string ProductName { get; init; }
    public string? Gtin { get; init; }
    public required string MatchType { get; init; } // "exact_gtin", "exact_name", "fuzzy", "manual"
    public required int MatchScore { get; init; } // 0-100
    public List<AlternativeMatch>? Alternatives { get; init; }
}

/// <summary>
/// Alternative product matches for ambiguous cases
/// </summary>
public record AlternativeMatch
{
    public required string ProductId { get; init; }
    public required string ProductName { get; init; }
    public string? Gtin { get; init; }
    public required int MatchScore { get; init; }
}

/// <summary>
/// Request to confirm and execute the import
/// </summary>
public record ExecuteImportRequest
{
    public required List<ConfirmedImportRow> Rows { get; init; }
}

/// <summary>
/// Confirmed row with user-selected product match
/// </summary>
public record ConfirmedImportRow
{
    public required int RowNumber { get; init; }
    public required string ProductId { get; init; }
    public required int Quantity { get; init; }
    public decimal? UnitCost { get; init; }
    public int? BonusQuantity { get; init; }
    public string? SerialNumber { get; init; }
    public DateTime? ExpiryDate { get; init; }
    public string? Notes { get; init; }
    
    // New product information (if user wants to create new product)
    public bool IsNewProduct { get; init; }
    public NewProductInfo? NewProduct { get; init; }
}

/// <summary>
/// New product information for products that don't exist in database
/// </summary>
public record NewProductInfo
{
    public required string ProductName { get; init; }
    public string? Gtin { get; init; }
    public string? Manufacturer { get; init; }
    public decimal? Price { get; init; }
    public string? Description { get; init; }
}

/// <summary>
/// Response after executing bulk import
/// </summary>
public record ExecuteImportResponse
{
    public required int TotalProcessed { get; init; }
    public required int Successful { get; init; }
    public required int Failed { get; init; }
    public List<ImportError>? Errors { get; init; }
}

/// <summary>
/// Error during import execution
/// </summary>
public record ImportError
{
    public required int RowNumber { get; init; }
    public required string Error { get; init; }
    public Dictionary<string, string?>? RowData { get; init; }
}

