namespace Opas.Shared.Customer;

/// <summary>
/// Customer DTO for API responses
/// </summary>
public record CustomerDto
{
    public required string Id { get; init; }
    public required string GlobalPatientId { get; init; }
    public required string CustomerType { get; init; }
    
    public string? TcNo { get; init; }
    public string? PassportNo { get; init; }
    
    public string? MotherTc { get; init; }
    public string? FatherTc { get; init; }
    
    public string? GuardianTc { get; init; }
    public string? GuardianName { get; init; }
    public string? GuardianPhone { get; init; }
    public string? GuardianRelation { get; init; }
    
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public required string Phone { get; init; }
    public DateTime? BirthDate { get; init; }
    public int? BirthYear { get; init; }
    public int? Age { get; init; }
    public string? Gender { get; init; }
    
    public string? City { get; init; }
    public string? District { get; init; }
    public string? Neighborhood { get; init; }
    public string? Street { get; init; }
    public string? BuildingNo { get; init; }
    public string? ApartmentNo { get; init; }
    
    public string? EmergencyContactName { get; init; }
    public string? EmergencyContactPhone { get; init; }
    public string? EmergencyContactRelation { get; init; }
    
    public string? Notes { get; init; }
    public bool KvkkConsent { get; init; }
    public DateTime? KvkkConsentDate { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public string CreatedBy { get; init; } = string.Empty;
}

/// <summary>
/// Create customer request
/// </summary>
public record CreateCustomerRequest
{
    public required string CustomerType { get; init; } // INDIVIDUAL, FOREIGN, INFANT
    
    public string? TcNo { get; init; }
    public string? PassportNo { get; init; }
    
    public string? MotherTc { get; init; }
    public string? FatherTc { get; init; }
    
    public string? GuardianTc { get; init; }
    public string? GuardianName { get; init; }
    public string? GuardianPhone { get; init; }
    public string? GuardianRelation { get; init; }
    
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public required string Phone { get; init; }
    public DateTime? BirthDate { get; init; }
    public string? Gender { get; init; }
    
    public string? City { get; init; }
    public string? District { get; init; }
    public string? Neighborhood { get; init; }
    public string? Street { get; init; }
    public string? BuildingNo { get; init; }
    public string? ApartmentNo { get; init; }
    
    public string? EmergencyContactName { get; init; }
    public string? EmergencyContactPhone { get; init; }
    public string? EmergencyContactRelation { get; init; }
    
    public string? Notes { get; init; }
    public bool KvkkConsent { get; init; }
}

/// <summary>
/// Update customer request
/// </summary>
public record UpdateCustomerRequest
{
    public string? TcNo { get; init; }
    public string? PassportNo { get; init; }
    
    public string? MotherTc { get; init; }
    public string? FatherTc { get; init; }
    
    public string? GuardianTc { get; init; }
    public string? GuardianName { get; init; }
    public string? GuardianPhone { get; init; }
    public string? GuardianRelation { get; init; }
    
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? Phone { get; init; }
    public DateTime? BirthDate { get; init; }
    public string? Gender { get; init; }
    
    public string? City { get; init; }
    public string? District { get; init; }
    public string? Neighborhood { get; init; }
    public string? Street { get; init; }
    public string? BuildingNo { get; init; }
    public string? ApartmentNo { get; init; }
    
    public string? EmergencyContactName { get; init; }
    public string? EmergencyContactPhone { get; init; }
    public string? EmergencyContactRelation { get; init; }
    
    public string? Notes { get; init; }
    public bool? KvkkConsent { get; init; }
    public bool? IsActive { get; init; }
}

/// <summary>
/// Customer search request
/// </summary>
public record CustomerSearchRequest
{
    public string? Query { get; init; } // İsim, soyisim, telefon, TC, passport
    public string? CustomerType { get; init; }
    public string? Gender { get; init; }
    public string? City { get; init; }
    public bool? IsActive { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 50;
}

/// <summary>
/// Customer search response
/// </summary>
public record CustomerSearchResponse
{
    public List<CustomerDto> Customers { get; init; } = new();
    public int Total { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
}
