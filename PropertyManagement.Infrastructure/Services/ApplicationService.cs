using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PropertyManagement.Domain.Entities;
using PropertyManagement.Domain.Enums;
using PropertyManagement.Domain.Services;
using PropertyManagement.Domain.Services.Validation;
using PropertyManagement.Infrastructure.Persistence;

namespace PropertyManagement.Infrastructure.Services;

public class ApplicationService(
    ApplicationDbContext db,
    UserManager<ApplicationUser> userManager,
    ApplicationWorkflowService workflow) : IApplicationService
{
    // IgnoreQueryFilters: removing a unit or property only hides it from browsing and management;
    // its applications and leases are kept, so the required Unit join must not drop them.
    // AsSplitQuery: Applicants, ResidenceHistoryEntries, StatusHistory and Unit.Leases are four
    // sibling/nested one-to-many collections. A single query joins all of them together, so an
    // application with 2 applicants would return each residence-history row twice (a SQL
    // cross-product) - e.g. a co-applicant application showing every residence entry doubled on
    // screen even though only one row exists in the database. Splitting into one query per
    // collection avoids the cross-product.
    private IQueryable<RentalApplication> FullGraph() => db.RentalApplications
        .IgnoreQueryFilters()
        .AsSplitQuery()
        .Include(a => a.Applicants).ThenInclude(x => x.User)
        .Include(a => a.ApplicantInformation)
        .Include(a => a.ResidenceHistoryEntries)
        .Include(a => a.StatusHistory).ThenInclude(h => h.ChangedByUser)
        .Include(a => a.Lease)
        .Include(a => a.Unit).ThenInclude(u => u!.Property)
        .Include(a => a.Unit).ThenInclude(u => u!.UnitType)
        .Include(a => a.Unit).ThenInclude(u => u!.Leases);

    public async Task<RentalApplication> StartOrResumeDraftAsync(int unitId, string applicantUserId, CancellationToken ct = default)
    {
        var existing = await FullGraph()
            .Where(a => a.UnitId == unitId && a.Applicants.Any(x => x.UserId == applicantUserId)
                        && (a.Status == ApplicationStatus.Draft || a.Status == ApplicationStatus.Returned
                            || a.Status == ApplicationStatus.Submitted))
            .FirstOrDefaultAsync(ct);

        if (existing is not null)
        {
            return existing;
        }

        var unit = await db.Units.FirstOrDefaultAsync(u => u.Id == unitId, ct)
            ?? throw new InvalidOperationException("Unit not found.");

        var application = new RentalApplication
        {
            UnitId = unit.Id,
            Status = ApplicationStatus.Draft,
            CurrentStep = ApplicationStep.ApplicantInformation
        };
        application.Applicants.Add(new ApplicationApplicant { UserId = applicantUserId, IsPrimary = true });
        application.StatusHistory.Add(new ApplicationStatusHistoryEntry
        {
            FromStatus = null,
            ToStatus = ApplicationStatus.Draft,
            ChangedByUserId = applicantUserId,
            ChangedAtUtc = DateTime.UtcNow
        });

        db.RentalApplications.Add(application);
        await db.SaveChangesAsync(ct);
        return application;
    }

    public async Task<RentalApplication?> GetForWizardAsync(int id, string userId, CancellationToken ct = default)
    {
        var application = await FullGraph().FirstOrDefaultAsync(a => a.Id == id, ct);
        return application is not null && ApplicationAccessPolicy.CanApplicantAccess(application, userId) ? application : null;
    }

    public async Task<RentalApplication?> GetDetailsAsync(int id, string userId, CancellationToken ct = default) =>
        await GetForWizardAsync(id, userId, ct);

    public Task<List<RentalApplication>> GetMyApplicationsAsync(string userId, ApplicationStatus? status, int? propertyId, CancellationToken ct = default)
    {
        var query = FullGraph().Where(a => a.Applicants.Any(x => x.UserId == userId));

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

    public async Task<PagedResult<RentalApplication>> GetMyApplicationsPagedAsync(string userId, ApplicationStatus? status, int? propertyId, int page, int pageSize, string? sortBy, bool descending, CancellationToken ct = default)
    {
        var query = FullGraph().Where(a => a.Applicants.Any(x => x.UserId == userId));

        if (status is not null)
        {
            query = query.Where(a => a.Status == status);
        }

        if (propertyId is not null)
        {
            query = query.Where(a => a.Unit!.PropertyId == propertyId);
        }

        var totalCount = await query.CountAsync(ct);

        query = sortBy?.ToLowerInvariant() switch
        {
            "status" => descending ? query.OrderByDescending(a => a.Status) : query.OrderBy(a => a.Status),
            _ => descending ? query.OrderByDescending(a => a.CreatedAtUtc) : query.OrderBy(a => a.CreatedAtUtc)
        };

        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return new PagedResult<RentalApplication>(items, totalCount, page, pageSize);
    }

    public async Task SaveApplicantInformationAsync(int applicationId, string userId, ApplicantInformationInput input, CancellationToken ct = default)
    {
        var application = await LoadEditableAsync(applicationId, userId, ct);

        var info = application.ApplicantInformation;
        if (info is null)
        {
            // Starts at 1: a form rendered before this row existed carries 0, so a second "first save" is rejected as stale.
            info = new ApplicantInformation { RentalApplicationId = applicationId, Version = 1 };
            db.ApplicantInformations.Add(info);
            application.ApplicantInformation = info;
        }
        else
        {
            db.Entry(info).Property(x => x.Version).OriginalValue = input.Version;
        }

        info.FullLegalName = input.FullLegalName;
        info.PhoneNumber = input.PhoneNumber;
        info.Email = input.Email;
        info.CurrentAddress = input.CurrentAddress;
        info.IsComplete = ApplicantInformationValidator.Validate(info).Count == 0;

        await db.SaveChangesAsync(ct);
    }

    public async Task SetStepAsync(int applicationId, string userId, ApplicationStep step, CancellationToken ct = default)
    {
        var application = await LoadEditableAsync(applicationId, userId, ct);
        application.CurrentStep = step;
        await db.SaveChangesAsync(ct);
    }

    public async Task SubmitAsync(int applicationId, string userId, CancellationToken ct = default)
    {
        var application = await LoadEditableAsync(applicationId, userId, ct);
        workflow.Submit(application, application.Unit!, userId, DateTime.UtcNow);
        await db.SaveChangesAsync(ct);
    }

    public async Task WithdrawAsync(int applicationId, string userId, CancellationToken ct = default)
    {
        var application = await FullGraph().FirstOrDefaultAsync(a => a.Id == applicationId, ct)
            ?? throw new ApplicationAccessDeniedException(applicationId);
        if (!ApplicationAccessPolicy.CanApplicantAccess(application, userId))
        {
            throw new ApplicationAccessDeniedException(applicationId);
        }

        workflow.Withdraw(application, userId, DateTime.UtcNow);
        await db.SaveChangesAsync(ct);
    }

    public async Task<ResidenceHistoryEntry> SaveResidenceEntryAsync(int applicationId, string userId, int? entryId, ResidenceHistoryInput input, CancellationToken ct = default)
    {
        var application = await LoadEditableAsync(applicationId, userId, ct);

        ResidenceHistoryEntry entry;
        if (entryId is int id)
        {
            entry = application.ResidenceHistoryEntries.FirstOrDefault(e => e.Id == id)
                ?? throw new InvalidOperationException("Residence history entry not found.");
            db.Entry(entry).Property(x => x.Version).OriginalValue = input.Version;
        }
        else
        {
            // A repeated post of the same form (double-click, retry) must not add the entry twice.
            var existing = application.ResidenceHistoryEntries.FirstOrDefault(e =>
                e.AddressLine1 == input.AddressLine1 && e.City == input.City && e.State == input.State && e.ZipCode == input.ZipCode &&
                e.LandlordName == input.LandlordName && e.LandlordPhone == input.LandlordPhone &&
                e.MoveInDate == input.MoveInDate && e.MoveOutDate == input.MoveOutDate);
            if (existing is not null)
            {
                return existing;
            }

            entry = new ResidenceHistoryEntry { RentalApplicationId = applicationId };
            db.ResidenceHistoryEntries.Add(entry);
            application.ResidenceHistoryEntries.Add(entry);
        }

        entry.AddressLine1 = input.AddressLine1;
        entry.City = input.City;
        entry.State = input.State;
        entry.ZipCode = input.ZipCode;
        entry.LandlordName = input.LandlordName;
        entry.LandlordPhone = input.LandlordPhone;
        entry.MoveInDate = input.MoveInDate;
        entry.MoveOutDate = input.MoveOutDate;

        await db.SaveChangesAsync(ct);
        return entry;
    }

    /// <summary>Reads the residence-history rows for a fragment re-render (e.g. right after
    /// SaveResidenceEntryAsync in the same request/DbContext). Deliberately does not go through
    /// GetForWizardAsync/FullGraph(): that query's Include(a => a.ResidenceHistoryEntries) targets
    /// the same tracked RentalApplication whose ResidenceHistoryEntries collection the save just
    /// added an entry to directly, and EF re-adds the row the identity map resolves to on top of
    /// it, duplicating it in the collection (not in the database - a second, unrelated query in a
    /// fresh context reads back the correct single row). Querying ResidenceHistoryEntries on its
    /// own avoids ever touching that collection a second time.</summary>
    public async Task<List<ResidenceHistoryEntry>> GetResidenceHistoryEntriesAsync(int applicationId, string userId, CancellationToken ct = default)
    {
        var applicants = await db.RentalApplications
            .IgnoreQueryFilters()
            .Where(a => a.Id == applicationId)
            .SelectMany(a => a.Applicants)
            .Select(a => a.UserId)
            .ToListAsync(ct);

        if (!applicants.Contains(userId))
        {
            throw new ApplicationAccessDeniedException(applicationId);
        }

        return await db.ResidenceHistoryEntries
            .Where(e => e.RentalApplicationId == applicationId)
            .OrderBy(e => e.Id)
            .ToListAsync(ct);
    }

    public async Task DeleteResidenceEntryAsync(int applicationId, string userId, int entryId, CancellationToken ct = default)
    {
        var application = await LoadEditableAsync(applicationId, userId, ct);
        var entry = application.ResidenceHistoryEntries.FirstOrDefault(e => e.Id == entryId);
        if (entry is null)
        {
            return;
        }

        db.ResidenceHistoryEntries.Remove(entry);
        await db.SaveChangesAsync(ct);
    }

    public async Task AddCoApplicantAsync(int applicationId, string requestingUserId, string newApplicantEmail, CancellationToken ct = default)
    {
        var application = await LoadEditableAsync(applicationId, requestingUserId, ct);

        var newUser = await userManager.FindByEmailAsync(newApplicantEmail)
            ?? throw new InvalidOperationException($"No user found with email '{newApplicantEmail}'.");

        if (application.Applicants.Any(a => a.UserId == newUser.Id))
        {
            return;
        }

        application.Applicants.Add(new ApplicationApplicant { RentalApplicationId = applicationId, UserId = newUser.Id, IsPrimary = false });
        await db.SaveChangesAsync(ct);
    }

    private async Task<RentalApplication> LoadEditableAsync(int applicationId, string userId, CancellationToken ct)
    {
        var application = await FullGraph().FirstOrDefaultAsync(a => a.Id == applicationId, ct)
            ?? throw new ApplicationAccessDeniedException(applicationId);

        if (!ApplicationAccessPolicy.CanApplicantAccess(application, userId))
        {
            throw new ApplicationAccessDeniedException(applicationId);
        }

        if (application.Status is not (ApplicationStatus.Draft or ApplicationStatus.Returned))
        {
            throw new InvalidOperationException("This application is not editable in its current status.");
        }

        return application;
    }
}
