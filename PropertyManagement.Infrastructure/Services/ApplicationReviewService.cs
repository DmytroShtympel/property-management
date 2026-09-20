using Microsoft.EntityFrameworkCore;
using PropertyManagement.Domain.Entities;
using PropertyManagement.Domain.Enums;
using PropertyManagement.Domain.Services;
using PropertyManagement.Infrastructure.Persistence;

namespace PropertyManagement.Infrastructure.Services;

public class ApplicationReviewService(
    ApplicationDbContext db,
    ApplicationWorkflowService workflow,
    ApplicationClaimService claimService) : IApplicationReviewService
{
    // IgnoreQueryFilters: a removed unit or property must not hide its applications (see ApplicationService).
    // AsSplitQuery: Applicants, ResidenceHistoryEntries, StatusHistory and Notes are sibling
    // one-to-many collections; one query joining all of them cross-products, so e.g. a
    // co-applicant application (2 Applicants rows) would show every residence-history and
    // status-history row twice even though only one row exists in the database.
    private IQueryable<RentalApplication> FullGraph() => db.RentalApplications
        .IgnoreQueryFilters()
        .AsSplitQuery()
        .Include(a => a.Applicants).ThenInclude(x => x.User)
        .Include(a => a.ApplicantInformation)
        .Include(a => a.ResidenceHistoryEntries)
        .Include(a => a.StatusHistory).ThenInclude(h => h.ChangedByUser)
        .Include(a => a.Notes).ThenInclude(n => n.Author)
        .Include(a => a.Lease)
        .Include(a => a.ClaimedByUser)
        .Include(a => a.Unit).ThenInclude(u => u!.Property)
        .Include(a => a.Unit).ThenInclude(u => u!.UnitType)
        .Include(a => a.Unit).ThenInclude(u => u!.Leases);

    // Managers see every application, not just those for properties they own — the listing
    // and review scope is intentionally global, unlike Property/Unit CRUD.
    public Task<List<RentalApplication>> GetAllAsync(ApplicationStatus? status, int? propertyId, CancellationToken ct = default)
    {
        var query = FullGraph();

        if (status is not null)
        {
            query = query.Where(a => a.Status == status);
        }

        if (propertyId is not null)
        {
            query = query.Where(a => a.Unit!.PropertyId == propertyId);
        }

        return query.OrderByDescending(a => a.CreatedAtUtc).ToListAsync(ct);
    }

    public async Task<PagedResult<RentalApplication>> GetAllPagedAsync(ApplicationStatus? status, int? propertyId, int page, int pageSize, string? sortBy, bool descending, CancellationToken ct = default)
    {
        var query = FullGraph();

        if (status is not null)
        {
            query = query.Where(a => a.Status == status);
        }

        if (propertyId is not null)
        {
            query = query.Where(a => a.Unit!.PropertyId == propertyId);
        }

        var totalCount = await query.CountAsync(ct);

        query = ApplySort(query, sortBy, descending);

        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return new PagedResult<RentalApplication>(items, totalCount, page, pageSize);
    }

    private static IQueryable<RentalApplication> ApplySort(IQueryable<RentalApplication> query, string? sortBy, bool descending) =>
        sortBy?.ToLowerInvariant() switch
        {
            "status" => descending ? query.OrderByDescending(a => a.Status) : query.OrderBy(a => a.Status),
            _ => descending ? query.OrderByDescending(a => a.CreatedAtUtc) : query.OrderBy(a => a.CreatedAtUtc)
        };

    public Task<List<RentalApplication>> GetQueueAsync(CancellationToken ct = default) =>
        FullGraph().Where(a => a.Status == ApplicationStatus.Submitted || a.Status == ApplicationStatus.UnderReview)
            .OrderBy(a => a.SubmittedAtUtc)
            .ToListAsync(ct);

    public Task<RentalApplication?> GetDetailsAsync(int id, CancellationToken ct = default) =>
        FullGraph().FirstOrDefaultAsync(a => a.Id == id, ct);

    public async Task ClaimAsync(int id, string managerUserId, CancellationToken ct = default)
    {
        var application = await RequireAsync(id, ct);
        var now = DateTime.UtcNow;
        claimService.Claim(application, managerUserId, now);
        workflow.MarkUnderReview(application, managerUserId, now);
        await db.SaveChangesAsync(ct);
    }

    public async Task ReleaseAsync(int id, string managerUserId, CancellationToken ct = default)
    {
        var application = await RequireAsync(id, ct);
        claimService.Release(application, managerUserId);
        workflow.ReturnToQueue(application, managerUserId, DateTime.UtcNow);
        await db.SaveChangesAsync(ct);
    }

    public async Task DecideAsync(int id, string managerUserId, ReviewOutcome outcome, string? comment, CancellationToken ct = default)
    {
        var application = await RequireAsync(id, ct);
        var now = DateTime.UtcNow;

        switch (outcome)
        {
            case ReviewOutcome.Approve:
                workflow.Approve(application, application.Unit!, managerUserId, now);
                break;
            case ReviewOutcome.Deny:
                workflow.Deny(application, managerUserId, comment!, now);
                break;
            case ReviewOutcome.Return:
                workflow.Return(application, managerUserId, comment!, now);
                break;
        }

        await db.SaveChangesAsync(ct);
    }

    public async Task AddNoteAsync(int id, string managerUserId, string body, CancellationToken ct = default)
    {
        var application = await RequireAsync(id, ct);
        application.Notes.Add(new ApplicationNote
        {
            RentalApplicationId = id,
            AuthorUserId = managerUserId,
            Body = body,
            CreatedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync(ct);
    }

    private async Task<RentalApplication> RequireAsync(int id, CancellationToken ct) =>
        await FullGraph().FirstOrDefaultAsync(a => a.Id == id, ct)
            ?? throw new InvalidOperationException($"Application {id} not found.");
}
