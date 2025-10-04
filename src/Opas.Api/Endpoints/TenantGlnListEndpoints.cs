using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Opas.Api.Endpoints;

public static class TenantGlnListEndpoints
{
    public static void MapTenantGlnListEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/tenant/gln-list")
            .WithTags("Tenant GLN List")
            .WithOpenApi();

        group.MapGet("/", GetGlnList)
            .WithName("GetGlnList")
            .WithSummary("Get GLN list from tenant database")
            .Produces<List<GlnListItemDto>>()
            .Produces(400)
            .Produces(500);
    }

    private static async Task<IResult> GetGlnList(
        [FromServices] IConfiguration configuration,
        HttpContext httpContext,
        CancellationToken ct = default)
    {
        try
        {
            // Get tenant ID from header
            var tenantId = httpContext.Request.Headers["x-tenant-id"].FirstOrDefault();
            if (string.IsNullOrEmpty(tenantId))
            {
                return Results.BadRequest("Tenant ID not found in headers");
            }

            // Extract GLN from tenant ID (TNT_<GLN> -> <GLN>)
            var gln = tenantId.Replace("TNT_", "");
            var tenantDbName = $"opas_tenant_{gln}";
            
            // Build tenant-specific connection string
            var baseConnectionString = configuration["Database:Postgres:ConnectionString"];
            var tenantConnectionString = baseConnectionString?.Replace("Database=opas_public", $"Database={tenantDbName}");

            // Query GLN list directly from tenant database
            using var connection = new NpgsqlConnection(tenantConnectionString);
            await connection.OpenAsync(ct);

            var sql = @"
                SELECT 
                    id, 
                    gln, 
                    COALESCE(company_name, '') as company_name,
                    COALESCE(authorized, '') as authorized,
                    COALESCE(email, '') as email,
                    COALESCE(phone, '') as phone,
                    COALESCE(city, '') as city,
                    COALESCE(town, '') as town,
                    COALESCE(address, '') as address,
                    active,
                    COALESCE(source, '') as source,
                    imported_at_utc
                FROM gln_list 
                WHERE active = true
                ORDER BY company_name";

            using var command = new NpgsqlCommand(sql, connection);
            using var reader = await command.ExecuteReaderAsync(ct);

            var glnList = new List<GlnListItemDto>();
            while (await reader.ReadAsync(ct))
            {
                glnList.Add(new GlnListItemDto
                {
                    Id = reader.GetInt32(0),           // id
                    Gln = reader.GetString(1),         // gln
                    CompanyName = reader.GetString(2), // company_name
                    Authorized = reader.GetString(3),  // authorized
                    Email = reader.GetString(4),       // email
                    Phone = reader.GetString(5),       // phone
                    City = reader.GetString(6),        // city
                    Town = reader.GetString(7),        // town
                    Address = reader.GetString(8),     // address
                    Active = reader.GetBoolean(9),     // active
                    Source = reader.GetString(10),     // source
                    ImportedAt = reader.GetDateTime(11).ToString("yyyy-MM-ddTHH:mm:ss.fffZ") // imported_at_utc
                });
            }

            return Results.Ok(glnList);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"GLN List Error: {ex.Message}");
            Console.WriteLine($"Stack Trace: {ex.StackTrace}");
            return Results.Problem(
                detail: $"Error fetching GLN list: {ex.Message}",
                statusCode: 500
            );
        }
    }
}

// DTO for GLN list items
public class GlnListItemDto
{
    public int Id { get; set; }
    public string Gln { get; set; } = default!;
    public string CompanyName { get; set; } = default!;
    public string Authorized { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string Phone { get; set; } = default!;
    public string City { get; set; } = default!;
    public string Town { get; set; } = default!;
    public string Address { get; set; } = default!;
    public bool Active { get; set; }
    public string Source { get; set; } = default!;
    public string ImportedAt { get; set; } = default!;
}
