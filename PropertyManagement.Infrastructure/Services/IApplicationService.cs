using PropertyManagement.Domain.Entities;
using PropertyManagement.Domain.Enums;

namespace PropertyManagement.Infrastructure.Services;

public record ApplicantInformationInput(string FullLegalName, string PhoneNumber, string Email, string CurrentAddress, int Version);

public record ResidenceHistoryInput(
    string AddressLine1, string City, string State, string ZipCode,
    string LandlordName, string LandlordPhone,
    DateOnly MoveInDate, DateOnly? MoveOutDate, int Version);

public class ApplicationAccessDeniedException(int applicationId) : Exception($"User does not have access to application {applicationId}.");

public interface IApplicationService
{
    Task<RentalApplication> StartOrResumeDraftAsync(int unitId, string applicantUserId, CancellationToken ct = default);
    Task<RentalApplication?> GetForWizardAsync(int id, string userId, CancellationToken ct = default);
    Task<RentalApplication?> GetDetailsAsync(int id, string userId, CancellationToken ct = default);
    Task<List<RentalApplication>> GetMyApplicationsAsync(string userId, ApplicationStatus? status, int? propertyId, CancellationToken ct = default);
    Task<PagedResult<RentalApplication>> GetMyApplicationsPagedAsync(string userId, ApplicationStatus? status, int? propertyId, int page, int pageSize, string? sortBy, bool descending, CancellationToken ct = default);

    Task SaveApplicantInformationAsync(int applicationId, string userId, ApplicantInformationInput input, CancellationToken ct = default);
    Task SetStepAsync(int applicationId, string userId, ApplicationStep step, CancellationToken ct = default);
    Task SubmitAsync(int applicationId, string userId, CancellationToken ct = default);
    Task WithdrawAsync(int applicationId, string userId, CancellationToken ct = default);

    Task<ResidenceHistoryEntry> SaveResidenceEntryAsync(int applicationId, string userId, int? entryId, ResidenceHistoryInput input, CancellationToken ct = default);
    Task DeleteResidenceEntryAsync(int applicationId, string userId, int entryId, CancellationToken ct = default);
    Task<List<ResidenceHistoryEntry>> GetResidenceHistoryEntriesAsync(int applicationId, string userId, CancellationToken ct = default);

    Task AddCoApplicantAsync(int applicationId, string requestingUserId, string newApplicantEmail, CancellationToken ct = default);
}
