using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Opas.Domain.Entities;
using Opas.Infrastructure.Logging;
using Opas.Shared.Customer;
using Opas.Shared.Logging;
using Opas.Infrastructure.Persistence;

namespace Opas.Api.Endpoints;

public static class CustomerEndpoints
{
    public static void MapCustomerEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/tenant/customers")
            .WithTags("Customers")
            .WithOpenApi();

        // GET /api/tenant/customers - List & Search customers
        group.MapGet("/", async (
            [AsParameters] TenantRequest tenantReq,
            IOpasLogger opasLogger,
            HttpContext httpContext,
            CancellationToken ct,
            [FromQuery] string? query = null,
            [FromQuery] string? customerType = null,
            [FromQuery] string? gender = null,
            [FromQuery] string? city = null,
            [FromQuery] bool? isActive = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50) =>
        {
            using (OpasLogContext.EnrichFromHttpContext(httpContext))
            {
                try
                {
                    var connStr = BuildTenantConnectionString(tenantReq.TenantId);
                    using var conn = new NpgsqlConnection(connStr);
                    await conn.OpenAsync(ct);

                    // Build query
                    var whereClauses = new List<string>();
                    var parameters = new List<NpgsqlParameter>();

                    if (!string.IsNullOrWhiteSpace(query))
                    {
                        whereClauses.Add("(first_name ILIKE @query OR last_name ILIKE @query OR phone LIKE @query OR tc_no LIKE @query OR passport_no LIKE @query)");
                        parameters.Add(new NpgsqlParameter("@query", $"%{query}%"));
                    }

                    if (!string.IsNullOrWhiteSpace(customerType))
                    {
                        whereClauses.Add("customer_type = @customerType");
                        parameters.Add(new NpgsqlParameter("@customerType", customerType));
                    }

                    if (!string.IsNullOrWhiteSpace(gender))
                    {
                        whereClauses.Add("gender = @gender");
                        parameters.Add(new NpgsqlParameter("@gender", gender));
                    }

                    if (!string.IsNullOrWhiteSpace(city))
                    {
                        whereClauses.Add("city = @city");
                        parameters.Add(new NpgsqlParameter("@city", city));
                    }

                    if (isActive.HasValue)
                    {
                        whereClauses.Add("is_active = @isActive");
                        parameters.Add(new NpgsqlParameter("@isActive", isActive.Value));
                    }

                    var whereClause = whereClauses.Count > 0 ? "WHERE " + string.Join(" AND ", whereClauses) : "";

                    // Count total
                    var countQuery = $"SELECT COUNT(*) FROM customers {whereClause}";
                    using var countCmd = new NpgsqlCommand(countQuery, conn);
                    countCmd.Parameters.AddRange(parameters.ToArray());
                    var total = Convert.ToInt32(await countCmd.ExecuteScalarAsync(ct));

                    // Get paginated results
                    var selectQuery = $@"
                        SELECT * FROM customers {whereClause}
                        ORDER BY created_at DESC
                        LIMIT @limit OFFSET @offset";

                    using var selectCmd = new NpgsqlCommand(selectQuery, conn);
                    selectCmd.Parameters.AddRange(parameters.Select(p => p.Clone()).Cast<NpgsqlParameter>().ToArray());
                    selectCmd.Parameters.AddWithValue("@limit", pageSize);
                    selectCmd.Parameters.AddWithValue("@offset", (page - 1) * pageSize);

                    var customers = new List<CustomerDto>();
                    using var reader = await selectCmd.ExecuteReaderAsync(ct);
                    while (await reader.ReadAsync(ct))
                    {
                        customers.Add(MapReaderToDto(reader));
                    }

                    opasLogger.LogDataAccess(tenantReq.Username, "Customer", "List", new { Query = query, Total = total });

                    return Results.Ok(new CustomerSearchResponse
                    {
                        Customers = customers,
                        Total = total,
                        Page = page,
                        PageSize = pageSize
                    });
                }
                catch (Exception ex)
                {
                    opasLogger.LogSystemEvent("Customer", "ListError", new { Error = ex.Message });
                    return Results.Problem(statusCode: 500, title: "Failed to list customers", detail: ex.Message);
                }
            }
        })
        .WithName("ListCustomers")
        .WithSummary("List and search customers");

        // GET /api/tenant/customers/{id} - Get customer by ID
        group.MapGet("/{id}", async (
            string id,
            [AsParameters] TenantRequest tenantReq,
            IOpasLogger opasLogger,
            HttpContext httpContext,
            CancellationToken ct) =>
        {
            using (OpasLogContext.EnrichFromHttpContext(httpContext))
            {
                try
                {
                    var connStr = BuildTenantConnectionString(tenantReq.TenantId);
                    using var conn = new NpgsqlConnection(connStr);
                    await conn.OpenAsync(ct);

                    var query = "SELECT * FROM customers WHERE id = @id";
                    using var cmd = new NpgsqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@id", id);

                    using var reader = await cmd.ExecuteReaderAsync(ct);
                    if (await reader.ReadAsync(ct))
                    {
                        var customer = MapReaderToDto(reader);
                        opasLogger.LogDataAccess(tenantReq.Username, "Customer", "Get", new { Id = id });
                        return Results.Ok(customer);
                    }

                    return Results.NotFound(new { error = "Customer not found" });
                }
                catch (Exception ex)
                {
                    opasLogger.LogSystemEvent("Customer", "GetError", new { Id = id, Error = ex.Message });
                    return Results.Problem(statusCode: 500, title: "Failed to get customer", detail: ex.Message);
                }
            }
        })
        .WithName("GetCustomer")
        .WithSummary("Get customer by ID");

        // POST /api/tenant/customers - Create customer
        group.MapPost("/", async (
            [FromBody] CreateCustomerRequest request,
            [AsParameters] TenantRequest tenantReq,
            IOpasLogger opasLogger,
            HttpContext httpContext,
            CancellationToken ct) =>
        {
            using (OpasLogContext.EnrichFromHttpContext(httpContext))
            {
                try
                {
                    // Validate
                    if (string.IsNullOrWhiteSpace(request.FirstName) || string.IsNullOrWhiteSpace(request.LastName))
                    {
                        return Results.BadRequest(new { error = "FirstName and LastName are required" });
                    }

                    // Generate Global Patient ID
                    var globalPatientId = CustomerIdGenerator.GenerateGlobalPatientId(
                        request.TcNo,
                        request.PassportNo,
                        request.MotherTc,
                        request.FatherTc,
                        request.BirthDate);

                    var connStr = BuildTenantConnectionString(tenantReq.TenantId);
                    using var conn = new NpgsqlConnection(connStr);
                    await conn.OpenAsync(ct);

                    // Get next sequence number for tenant-specific ID
                    var seqQuery = "SELECT COALESCE(MAX(CAST(SUBSTRING(id FROM 20) AS INT)), 0) + 1 FROM customers";
                    using var seqCmd = new NpgsqlCommand(seqQuery, conn);
                    var nextSeq = Convert.ToInt32(await seqCmd.ExecuteScalarAsync(ct));

                    // Generate tenant-specific ID
                    var gln = tenantReq.TenantId.Replace("TNT_", "");
                    var tenantCustomerId = CustomerIdGenerator.GenerateTenantCustomerId(
                        request.FirstName,
                        request.LastName,
                        gln,
                        nextSeq);

                    // Calculate age
                    int? age = request.BirthDate.HasValue
                        ? CustomerIdGenerator.CalculateAge(request.BirthDate.Value)
                        : null;

                    var birthYear = request.BirthDate?.Year;

                    // Insert
                    var insertQuery = @"
                        INSERT INTO customers (
                            id, global_patient_id, customer_type,
                            tc_no, passport_no,
                            mother_tc, father_tc,
                            guardian_tc, guardian_name, guardian_phone, guardian_relation,
                            first_name, last_name, phone, birth_date, birth_year, age, gender,
                            city, district, neighborhood, street, building_no, apartment_no,
                            emergency_contact_name, emergency_contact_phone, emergency_contact_relation,
                            notes, kvkk_consent, kvkk_consent_date, is_active, created_by
                        ) VALUES (
                            @id, @globalPatientId, @customerType,
                            @tcNo, @passportNo,
                            @motherTc, @fatherTc,
                            @guardianTc, @guardianName, @guardianPhone, @guardianRelation,
                            @firstName, @lastName, @phone, @birthDate, @birthYear, @age, @gender,
                            @city, @district, @neighborhood, @street, @buildingNo, @apartmentNo,
                            @emergencyContactName, @emergencyContactPhone, @emergencyContactRelation,
                            @notes, @kvkkConsent, @kvkkConsentDate, TRUE, @createdBy
                        ) RETURNING *";

                    using var cmd = new NpgsqlCommand(insertQuery, conn);
                    cmd.Parameters.AddWithValue("@id", tenantCustomerId);
                    cmd.Parameters.AddWithValue("@globalPatientId", globalPatientId);
                    cmd.Parameters.AddWithValue("@customerType", request.CustomerType);
                    cmd.Parameters.AddWithValue("@tcNo", (object?)request.TcNo ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@passportNo", (object?)request.PassportNo ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@motherTc", (object?)request.MotherTc ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@fatherTc", (object?)request.FatherTc ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@guardianTc", (object?)request.GuardianTc ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@guardianName", (object?)request.GuardianName ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@guardianPhone", (object?)request.GuardianPhone ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@guardianRelation", (object?)request.GuardianRelation ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@firstName", request.FirstName);
                    cmd.Parameters.AddWithValue("@lastName", request.LastName);
                    cmd.Parameters.AddWithValue("@phone", request.Phone);
                    cmd.Parameters.AddWithValue("@birthDate", (object?)request.BirthDate ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@birthYear", (object?)birthYear ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@age", (object?)age ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@gender", (object?)request.Gender ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@city", (object?)request.City ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@district", (object?)request.District ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@neighborhood", (object?)request.Neighborhood ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@street", (object?)request.Street ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@buildingNo", (object?)request.BuildingNo ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@apartmentNo", (object?)request.ApartmentNo ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@emergencyContactName", (object?)request.EmergencyContactName ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@emergencyContactPhone", (object?)request.EmergencyContactPhone ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@emergencyContactRelation", (object?)request.EmergencyContactRelation ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@notes", (object?)request.Notes ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@kvkkConsent", request.KvkkConsent);
                    cmd.Parameters.AddWithValue("@kvkkConsentDate", request.KvkkConsent ? DateTime.UtcNow : DBNull.Value);
                    cmd.Parameters.AddWithValue("@createdBy", tenantReq.Username);

                    using var reader = await cmd.ExecuteReaderAsync(ct);
                    if (await reader.ReadAsync(ct))
                    {
                        var customer = MapReaderToDto(reader);
                        opasLogger.LogDataAccess(tenantReq.Username, "Customer", "Create", new { Id = customer.Id });
                        return Results.Created($"/api/tenant/customers/{customer.Id}", customer);
                    }

                    return Results.Problem("Failed to create customer");
                }
                catch (Exception ex)
                {
                    opasLogger.LogSystemEvent("Customer", "CreateError", new { Error = ex.Message });
                    return Results.Problem(statusCode: 500, title: "Failed to create customer", detail: ex.Message);
                }
            }
        })
        .WithName("CreateCustomer")
        .WithSummary("Create a new customer");

        // PUT /api/tenant/customers/{id} - Update customer
        group.MapPut("/{id}", async (
            string id,
            [FromBody] UpdateCustomerRequest request,
            [AsParameters] TenantRequest tenantReq,
            IOpasLogger opasLogger,
            HttpContext httpContext,
            CancellationToken ct) =>
        {
            using (OpasLogContext.EnrichFromHttpContext(httpContext))
            {
                try
                {
                    var connStr = BuildTenantConnectionString(tenantReq.TenantId);
                    using var conn = new NpgsqlConnection(connStr);
                    await conn.OpenAsync(ct);

                    // Build dynamic UPDATE query
                    var updates = new List<string>();
                    var parameters = new List<NpgsqlParameter> { new("@id", id) };

                    if (request.TcNo != null) { updates.Add("tc_no = @tcNo"); parameters.Add(new("@tcNo", request.TcNo)); }
                    if (request.PassportNo != null) { updates.Add("passport_no = @passportNo"); parameters.Add(new("@passportNo", request.PassportNo)); }
                    if (request.MotherTc != null) { updates.Add("mother_tc = @motherTc"); parameters.Add(new("@motherTc", request.MotherTc)); }
                    if (request.FatherTc != null) { updates.Add("father_tc = @fatherTc"); parameters.Add(new("@fatherTc", request.FatherTc)); }
                    if (request.GuardianName != null) { updates.Add("guardian_name = @guardianName"); parameters.Add(new("@guardianName", request.GuardianName)); }
                    if (request.GuardianPhone != null) { updates.Add("guardian_phone = @guardianPhone"); parameters.Add(new("@guardianPhone", request.GuardianPhone)); }
                    if (request.FirstName != null) { updates.Add("first_name = @firstName"); parameters.Add(new("@firstName", request.FirstName)); }
                    if (request.LastName != null) { updates.Add("last_name = @lastName"); parameters.Add(new("@lastName", request.LastName)); }
                    if (request.Phone != null) { updates.Add("phone = @phone"); parameters.Add(new("@phone", request.Phone)); }
                    if (request.BirthDate != null)
                    {
                        updates.Add("birth_date = @birthDate");
                        updates.Add("birth_year = @birthYear");
                        updates.Add("age = @age");
                        parameters.Add(new("@birthDate", request.BirthDate));
                        parameters.Add(new("@birthYear", request.BirthDate.Value.Year));
                        parameters.Add(new("@age", CustomerIdGenerator.CalculateAge(request.BirthDate.Value)));
                    }
                    if (request.Gender != null) { updates.Add("gender = @gender"); parameters.Add(new("@gender", request.Gender)); }
                    if (request.City != null) { updates.Add("city = @city"); parameters.Add(new("@city", request.City)); }
                    if (request.District != null) { updates.Add("district = @district"); parameters.Add(new("@district", request.District)); }
                    if (request.Neighborhood != null) { updates.Add("neighborhood = @neighborhood"); parameters.Add(new("@neighborhood", request.Neighborhood)); }
                    if (request.Street != null) { updates.Add("street = @street"); parameters.Add(new("@street", request.Street)); }
                    if (request.BuildingNo != null) { updates.Add("building_no = @buildingNo"); parameters.Add(new("@buildingNo", request.BuildingNo)); }
                    if (request.ApartmentNo != null) { updates.Add("apartment_no = @apartmentNo"); parameters.Add(new("@apartmentNo", request.ApartmentNo)); }
                    if (request.EmergencyContactName != null) { updates.Add("emergency_contact_name = @emergencyContactName"); parameters.Add(new("@emergencyContactName", request.EmergencyContactName)); }
                    if (request.EmergencyContactPhone != null) { updates.Add("emergency_contact_phone = @emergencyContactPhone"); parameters.Add(new("@emergencyContactPhone", request.EmergencyContactPhone)); }
                    if (request.EmergencyContactRelation != null) { updates.Add("emergency_contact_relation = @emergencyContactRelation"); parameters.Add(new("@emergencyContactRelation", request.EmergencyContactRelation)); }
                    if (request.Notes != null) { updates.Add("notes = @notes"); parameters.Add(new("@notes", request.Notes)); }
                    if (request.KvkkConsent.HasValue)
                    {
                        updates.Add("kvkk_consent = @kvkkConsent");
                        updates.Add("kvkk_consent_date = @kvkkConsentDate");
                        parameters.Add(new("@kvkkConsent", request.KvkkConsent.Value));
                        parameters.Add(new("@kvkkConsentDate", request.KvkkConsent.Value ? DateTime.UtcNow : DBNull.Value));
                    }
                    if (request.IsActive.HasValue) { updates.Add("is_active = @isActive"); parameters.Add(new("@isActive", request.IsActive.Value)); }

                    if (updates.Count == 0)
                    {
                        return Results.BadRequest(new { error = "No fields to update" });
                    }

                    var updateQuery = $"UPDATE customers SET {string.Join(", ", updates)} WHERE id = @id RETURNING *";
                    using var cmd = new NpgsqlCommand(updateQuery, conn);
                    cmd.Parameters.AddRange(parameters.ToArray());

                    using var reader = await cmd.ExecuteReaderAsync(ct);
                    if (await reader.ReadAsync(ct))
                    {
                        var customer = MapReaderToDto(reader);
                        opasLogger.LogDataAccess(tenantReq.Username, "Customer", "Update", new { Id = id });
                        return Results.Ok(customer);
                    }

                    return Results.NotFound(new { error = "Customer not found" });
                }
                catch (Exception ex)
                {
                    opasLogger.LogSystemEvent("Customer", "UpdateError", new { Id = id, Error = ex.Message });
                    return Results.Problem(statusCode: 500, title: "Failed to update customer", detail: ex.Message);
                }
            }
        })
        .WithName("UpdateCustomer")
        .WithSummary("Update customer");

        // DELETE /api/tenant/customers/{id} - Delete (soft delete)
        group.MapDelete("/{id}", async (
            string id,
            [AsParameters] TenantRequest tenantReq,
            IOpasLogger opasLogger,
            HttpContext httpContext,
            CancellationToken ct) =>
        {
            using (OpasLogContext.EnrichFromHttpContext(httpContext))
            {
                try
                {
                    var connStr = BuildTenantConnectionString(tenantReq.TenantId);
                    using var conn = new NpgsqlConnection(connStr);
                    await conn.OpenAsync(ct);

                    var query = "UPDATE customers SET is_active = FALSE WHERE id = @id RETURNING *";
                    using var cmd = new NpgsqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@id", id);

                    using var reader = await cmd.ExecuteReaderAsync(ct);
                    if (await reader.ReadAsync(ct))
                    {
                        opasLogger.LogDataAccess(tenantReq.Username, "Customer", "Delete", new { Id = id });
                        return Results.Ok(new { success = true, message = "Customer deleted (soft delete)" });
                    }

                    return Results.NotFound(new { error = "Customer not found" });
                }
                catch (Exception ex)
                {
                    opasLogger.LogSystemEvent("Customer", "DeleteError", new { Id = id, Error = ex.Message });
                    return Results.Problem(statusCode: 500, title: "Failed to delete customer", detail: ex.Message);
                }
            }
        })
        .WithName("DeleteCustomer")
        .WithSummary("Delete customer (soft delete)");

        // POST /api/tenant/customers/seed-mock-data - Seed 600 mock customers
        group.MapPost("/seed-mock-data", async (
            [AsParameters] TenantRequest tenantReq,
            IOpasLogger opasLogger,
            HttpContext httpContext,
            CancellationToken ct) =>
        {
            using (OpasLogContext.EnrichFromHttpContext(httpContext))
            {
                try
                {
                    var connStr = BuildTenantConnectionString(tenantReq.TenantId);
                    using var conn = new NpgsqlConnection(connStr);
                    await conn.OpenAsync(ct);

                    // Generate 600 mock customers
                    var mockCustomers = CustomerMockData.Generate500Customers(tenantReq.TenantId);
                    var gln = tenantReq.TenantId.Replace("TNT_", "");
                    
                    var createdCount = 0;
                    var errors = new List<string>();

                    // Get initial sequence number ONCE
                    var seqQuery = "SELECT COALESCE(MAX(CAST(SUBSTRING(id FROM 20) AS INT)), 0) + 1 FROM customers";
                    using var seqCmd = new NpgsqlCommand(seqQuery, conn);
                    var nextSeq = Convert.ToInt32(await seqCmd.ExecuteScalarAsync(ct));

                    foreach (var request in mockCustomers)
                    {
                        try
                        {
                            // Generate Global Patient ID
                            var globalPatientId = CustomerIdGenerator.GenerateGlobalPatientId(
                                request.TcNo,
                                request.PassportNo,
                                request.MotherTc,
                                request.FatherTc,
                                request.BirthDate);

                            // Generate tenant-specific ID with incrementing sequence
                            var tenantCustomerId = CustomerIdGenerator.GenerateTenantCustomerId(
                                request.FirstName,
                                request.LastName,
                                gln,
                                nextSeq);
                            
                            nextSeq++; // ✅ Her customer için sequence artır

                            // Calculate age
                            int? age = request.BirthDate.HasValue
                                ? CustomerIdGenerator.CalculateAge(request.BirthDate.Value)
                                : null;

                            var birthYear = request.BirthDate?.Year;

                            // Insert
                            var insertQuery = @"
                                INSERT INTO customers (
                                    id, global_patient_id, customer_type,
                                    tc_no, passport_no,
                                    mother_tc, father_tc,
                                    guardian_tc, guardian_name, guardian_phone, guardian_relation,
                                    first_name, last_name, phone, birth_date, birth_year, age, gender,
                                    city, district, neighborhood, street, building_no, apartment_no,
                                    emergency_contact_name, emergency_contact_phone, emergency_contact_relation,
                                    notes, kvkk_consent, kvkk_consent_date, is_active, created_by
                                ) VALUES (
                                    @id, @globalPatientId, @customerType,
                                    @tcNo, @passportNo,
                                    @motherTc, @fatherTc,
                                    @guardianTc, @guardianName, @guardianPhone, @guardianRelation,
                                    @firstName, @lastName, @phone, @birthDate, @birthYear, @age, @gender,
                                    @city, @district, @neighborhood, @street, @buildingNo, @apartmentNo,
                                    @emergencyContactName, @emergencyContactPhone, @emergencyContactRelation,
                                    @notes, @kvkkConsent, @kvkkConsentDate, TRUE, @createdBy
                                )";

                            using var cmd = new NpgsqlCommand(insertQuery, conn);
                            cmd.Parameters.AddWithValue("@id", tenantCustomerId);
                            cmd.Parameters.AddWithValue("@globalPatientId", globalPatientId);
                            cmd.Parameters.AddWithValue("@customerType", request.CustomerType);
                            cmd.Parameters.AddWithValue("@tcNo", (object?)request.TcNo ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@passportNo", (object?)request.PassportNo ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@motherTc", (object?)request.MotherTc ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@fatherTc", (object?)request.FatherTc ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@guardianTc", (object?)request.GuardianTc ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@guardianName", (object?)request.GuardianName ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@guardianPhone", (object?)request.GuardianPhone ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@guardianRelation", (object?)request.GuardianRelation ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@firstName", request.FirstName);
                            cmd.Parameters.AddWithValue("@lastName", request.LastName);
                            cmd.Parameters.AddWithValue("@phone", request.Phone);
                            cmd.Parameters.AddWithValue("@birthDate", (object?)request.BirthDate ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@birthYear", (object?)birthYear ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@age", (object?)age ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@gender", (object?)request.Gender ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@city", (object?)request.City ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@district", (object?)request.District ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@neighborhood", (object?)request.Neighborhood ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@street", (object?)request.Street ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@buildingNo", (object?)request.BuildingNo ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@apartmentNo", (object?)request.ApartmentNo ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@emergencyContactName", (object?)request.EmergencyContactName ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@emergencyContactPhone", (object?)request.EmergencyContactPhone ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@emergencyContactRelation", (object?)request.EmergencyContactRelation ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@notes", (object?)request.Notes ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@kvkkConsent", request.KvkkConsent);
                            cmd.Parameters.AddWithValue("@kvkkConsentDate", request.KvkkConsent ? DateTime.UtcNow : DBNull.Value);
                            cmd.Parameters.AddWithValue("@createdBy", "SYSTEM_MOCK_DATA");

                            await cmd.ExecuteNonQueryAsync(ct);
                            createdCount++;
                        }
                        catch (Exception ex)
                        {
                            errors.Add($"Error creating {request.FirstName} {request.LastName}: {ex.Message}");
                        }
                    }

                    opasLogger.LogDataAccess(tenantReq.Username, "Customer", "SeedMockData", new { 
                        TenantId = tenantReq.TenantId,
                        Created = createdCount,
                        Errors = errors.Count
                    });

                    return Results.Ok(new
                    {
                        success = true,
                        message = $"Mock data seeded successfully",
                        created = createdCount,
                        total = mockCustomers.Count,
                        errors = errors.Count > 0 ? errors : null
                    });
                }
                catch (Exception ex)
                {
                    opasLogger.LogSystemEvent("Customer", "SeedMockDataError", new { Error = ex.Message });
                    return Results.Problem(statusCode: 500, title: "Failed to seed mock data", detail: ex.Message);
                }
            }
        })
        .WithName("SeedMockCustomers")
        .WithSummary("Seed 600 mock customers (450 normal, 40 foreign, 10 infant, 100 children 0-18)");
    }

    // Helper: Build tenant connection string
    private static string BuildTenantConnectionString(string tenantId)
    {
        var gln = tenantId.Replace("TNT_", "");
        return $"Host=127.0.0.1;Port=5432;Database=opas_tenant_{gln};Username=postgres;Password=postgres";
    }

    // Helper: Map DB reader to DTO
    private static CustomerDto MapReaderToDto(NpgsqlDataReader reader)
    {
        return new CustomerDto
        {
            Id = reader.GetString(0),
            GlobalPatientId = reader.GetString(1),
            CustomerType = reader.GetString(2),
            TcNo = reader.IsDBNull(3) ? null : reader.GetString(3),
            PassportNo = reader.IsDBNull(4) ? null : reader.GetString(4),
            MotherTc = reader.IsDBNull(5) ? null : reader.GetString(5),
            FatherTc = reader.IsDBNull(6) ? null : reader.GetString(6),
            GuardianTc = reader.IsDBNull(7) ? null : reader.GetString(7),
            GuardianName = reader.IsDBNull(8) ? null : reader.GetString(8),
            GuardianPhone = reader.IsDBNull(9) ? null : reader.GetString(9),
            GuardianRelation = reader.IsDBNull(10) ? null : reader.GetString(10),
            FirstName = reader.GetString(11),
            LastName = reader.GetString(12),
            Phone = reader.GetString(13),
            BirthDate = reader.IsDBNull(14) ? null : reader.GetDateTime(14),
            BirthYear = reader.IsDBNull(15) ? null : reader.GetInt32(15),
            Age = reader.IsDBNull(16) ? null : reader.GetInt32(16),
            Gender = reader.IsDBNull(17) ? null : reader.GetString(17),
            City = reader.IsDBNull(18) ? null : reader.GetString(18),
            District = reader.IsDBNull(19) ? null : reader.GetString(19),
            Neighborhood = reader.IsDBNull(20) ? null : reader.GetString(20),
            Street = reader.IsDBNull(21) ? null : reader.GetString(21),
            BuildingNo = reader.IsDBNull(22) ? null : reader.GetString(22),
            ApartmentNo = reader.IsDBNull(23) ? null : reader.GetString(23),
            EmergencyContactName = reader.IsDBNull(24) ? null : reader.GetString(24),
            EmergencyContactPhone = reader.IsDBNull(25) ? null : reader.GetString(25),
            EmergencyContactRelation = reader.IsDBNull(26) ? null : reader.GetString(26),
            Notes = reader.IsDBNull(27) ? null : reader.GetString(27),
            KvkkConsent = reader.GetBoolean(28),
            KvkkConsentDate = reader.IsDBNull(29) ? null : reader.GetDateTime(29),
            IsActive = reader.GetBoolean(30),
            CreatedAt = reader.GetDateTime(31),
            UpdatedAt = reader.GetDateTime(32),
            CreatedBy = reader.GetString(33)
        };
    }
}

// Request model for tenant context
public record TenantRequest(
    [FromHeader(Name = "X-TenantId")] string TenantId,
    [FromHeader(Name = "X-Username")] string Username
);

