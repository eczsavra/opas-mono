using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using OfficeOpenXml;
using System.Globalization;

namespace Opas.Infrastructure.Services;

/// <summary>
/// Service for parsing various file formats (Excel, CSV, TSV)
/// </summary>
public class FileParserService
{
    static FileParserService()
    {
        // Set EPPlus license (required for EPPlus 8+)
        // License is set via configuration or environment variable
        // For non-commercial use, no code is needed if using free version
    }

    /// <summary>
    /// Parse file and extract raw data as a table
    /// </summary>
    public async Task<ParsedFileData> ParseFileAsync(string fileName, string fileType, byte[] content)
    {
        return fileType.ToLowerInvariant() switch
        {
            "excel" or ".xlsx" or ".xls" => await ParseExcelAsync(content),
            "csv" or ".csv" => await ParseCsvAsync(content, ','),
            "tsv" or ".tsv" or ".txt" => await ParseCsvAsync(content, '\t'),
            _ => throw new NotSupportedException($"File type '{fileType}' is not supported yet.")
        };
    }

    private async Task<ParsedFileData> ParseExcelAsync(byte[] content)
    {
        using var stream = new MemoryStream(content);
        using var package = new ExcelPackage(stream);

        var worksheet = package.Workbook.Worksheets.FirstOrDefault()
            ?? throw new InvalidOperationException("Excel file has no worksheets.");

        var result = new ParsedFileData();

        // Read header row (row 1)
        var headers = new List<string>();
        for (int col = 1; col <= worksheet.Dimension.End.Column; col++)
        {
            var headerValue = worksheet.Cells[1, col].Text?.Trim() ?? $"Column{col}";
            headers.Add(headerValue);
        }
        result.Headers = headers;

        // Read data rows (starting from row 2)
        for (int row = 2; row <= worksheet.Dimension.End.Row; row++)
        {
            var rowData = new Dictionary<string, string?>();
            bool isEmptyRow = true;

            for (int col = 1; col <= worksheet.Dimension.End.Column; col++)
            {
                var cellValue = worksheet.Cells[row, col].Text?.Trim();
                if (!string.IsNullOrWhiteSpace(cellValue))
                {
                    isEmptyRow = false;
                }
                rowData[headers[col - 1]] = cellValue;
            }

            // Skip completely empty rows
            if (!isEmptyRow)
            {
                result.Rows.Add(rowData);
            }
        }

        return await Task.FromResult(result);
    }

    private async Task<ParsedFileData> ParseCsvAsync(byte[] content, char delimiter)
    {
        using var stream = new MemoryStream(content);
        using var reader = new StreamReader(stream, Encoding.UTF8);

        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = delimiter.ToString(),
            HasHeaderRecord = true,
            MissingFieldFound = null,
            BadDataFound = null,
            TrimOptions = TrimOptions.Trim
        };

        using var csv = new CsvReader(reader, config);

        var result = new ParsedFileData();

        // Read header
        await csv.ReadAsync();
        csv.ReadHeader();
        result.Headers = csv.HeaderRecord?.ToList() ?? [];

        // Read data rows
        while (await csv.ReadAsync())
        {
            var rowData = new Dictionary<string, string?>();
            foreach (var header in result.Headers)
            {
                var value = csv.GetField(header)?.Trim();
                rowData[header] = string.IsNullOrWhiteSpace(value) ? null : value;
            }

            // Skip completely empty rows
            if (rowData.Values.Any(v => !string.IsNullOrWhiteSpace(v)))
            {
                result.Rows.Add(rowData);
            }
        }

        return result;
    }
}

/// <summary>
/// Parsed file data structure
/// </summary>
public class ParsedFileData
{
    public List<string> Headers { get; set; } = [];
    public List<Dictionary<string, string?>> Rows { get; set; } = [];
}

