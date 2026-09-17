using PropertyManagement.Domain.Enums;

namespace PropertyManagement.Web.Models.Api;

/// <summary>Row shape returned by GET /api/applications, consumed by the reusable applications
/// grid view component.</summary>
public record ApplicationApiDto(
    int Id,
    string PropertyName,
    string UnitNumber,
    ApplicationStatus Status,
    DateTime CreatedAtUtc,
    string ApplicantNames);

public record PagedApiResult<T>(List<T> Rows, int TotalCount, int Page, int PageSize);
