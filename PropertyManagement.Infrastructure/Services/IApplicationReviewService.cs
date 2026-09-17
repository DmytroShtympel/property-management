using PropertyManagement.Domain.Entities;
using PropertyManagement.Domain.Enums;

namespace PropertyManagement.Infrastructure.Services;

public enum ReviewOutcome { Approve, Deny, Return }

public interface IApplicationReviewService
{
    Task<List<RentalApplication>> GetAllAsync(ApplicationStatus? status, int? propertyId, CancellationToken ct = default);
    Task<PagedResult<RentalApplication>> GetAllPagedAsync(ApplicationStatus? status, int? propertyId, int page, int pageSize, string? sortBy, bool descending, CancellationToken ct = default);
    Task<List<RentalApplication>> GetQueueAsync(CancellationToken ct = default);
    Task<RentalApplication?> GetDetailsAsync(int id, CancellationToken ct = default);

    Task ClaimAsync(int id, string managerUserId, CancellationToken ct = default);
    Task ReleaseAsync(int id, string managerUserId, CancellationToken ct = default);
    Task DecideAsync(int id, string managerUserId, ReviewOutcome outcome, string? comment, CancellationToken ct = default);
    Task AddNoteAsync(int id, string managerUserId, string body, CancellationToken ct = default);
}
