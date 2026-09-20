using Microsoft.EntityFrameworkCore;
using PropertyManagement.Domain.Entities;
using PropertyManagement.Domain.Enums;
using PropertyManagement.Infrastructure.Persistence;
using PropertyManagement.Infrastructure.Services;

namespace PropertyManagement.Tests.Infrastructure;

// Regression test for a real bug: adding a residence-history entry showed it twice in the list
// right after saving, even though the database only ever had one row.
//
// ResidenceHistorySave (the POST action) and the fragment re-render that follows it in the same
// request both went through GetForWizardAsync/FullGraph() on the same scoped DbContext.
// SaveResidenceEntryAsync adds the new entry straight into the tracked RentalApplication's
// ResidenceHistoryEntries collection; a second FullGraph() query in the same request/context then
// resolves that same application through EF's identity map and re-adds the row its Include
// returns on top of the one already sitting in the collection - one row in SQL, two references
// in the in-memory collection. EF's InMemory provider doesn't reproduce this (no real Include
// fixup against a relational identity map), so this needs LocalDB.
public class ResidenceHistorySplitQueryTests : IAsyncLifetime
{
    private readonly string _databaseName = $"PropertyManagementDb_Test_{Guid.NewGuid():N}";
    private string ConnectionString => $"Server=(localdb)\\mssqllocaldb;Database={_databaseName};Trusted_Connection=True;TrustServerCertificate=True";

    private ApplicationDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(ConnectionString).Options);

    public async Task InitializeAsync()
    {
        await using var db = CreateContext();
        await db.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await using var db = CreateContext();
        await db.Database.EnsureDeletedAsync();
    }

    private async Task<int> SeedDraftWithTwoApplicantsAsync(ApplicationDbContext db)
    {
        var applicant1 = new ApplicationUser { Id = "applicant-1", UserName = "applicant1@demo.local", Email = "applicant1@demo.local" };
        var applicant2 = new ApplicationUser { Id = "applicant-2", UserName = "applicant2@demo.local", Email = "applicant2@demo.local" };
        var manager = new ApplicationUser { Id = "manager-1", UserName = "manager1@demo.local", Email = "manager1@demo.local" };
        var unitType = new UnitType { Name = "Studio", IsActive = true };
        var property = new Property { OwnerUserId = manager.Id, Name = "Prop", AddressLine1 = "1 St", City = "C", State = "S", ZipCode = "00000" };
        var unit = new Unit { Property = property, UnitType = unitType, UnitNumber = "1A", Bedrooms = 0, RentAmount = 1000m };
        var application = new RentalApplication { Unit = unit, Status = ApplicationStatus.Draft };
        application.Applicants.Add(new ApplicationApplicant { UserId = applicant1.Id, IsPrimary = true });
        application.Applicants.Add(new ApplicationApplicant { UserId = applicant2.Id, IsPrimary = false });

        db.Users.AddRange(applicant1, applicant2, manager);
        db.RentalApplications.Add(application);
        await db.SaveChangesAsync();
        return application.Id;
    }

    private static ResidenceHistoryInput Entry() =>
        new("9 Oak St", "Springfield", "IL", "62701", "Pat Owner", "555-000-1111", new DateOnly(2024, 6, 1), null, Version: 0);

    [Fact]
    public async Task SavingThenReReadingEntriesInTheSameRequest_ReturnsTheEntryOnce()
    {
        await using var db = CreateContext();
        var applicationId = await SeedDraftWithTwoApplicantsAsync(db);
        var sut = new ApplicationService(db, userManager: null!, workflow: null!);

        // Mirrors ResidenceHistorySave: save the new entry, then (in the same request/context)
        // fetch what the fragment shows, the way ResidenceHistoryFragmentAsync now does.
        await sut.SaveResidenceEntryAsync(applicationId, "applicant-1", entryId: null, Entry());
        var entries = await sut.GetResidenceHistoryEntriesAsync(applicationId, "applicant-1");

        Assert.Single(entries);
    }

    [Fact]
    public async Task SavingThenReloadingTheFullGraphInTheSameRequest_DuplicatesTheEntry()
    {
        // Documents the bug this regression guards against: re-running GetForWizardAsync (not
        // GetResidenceHistoryEntriesAsync) on the same context right after a save is exactly the
        // pattern ResidenceHistoryFragmentAsync used to follow, and it really does duplicate the
        // entry in the tracked collection - one row in the database, two in the collection.
        await using var db = CreateContext();
        var applicationId = await SeedDraftWithTwoApplicantsAsync(db);
        var sut = new ApplicationService(db, userManager: null!, workflow: null!);

        await sut.SaveResidenceEntryAsync(applicationId, "applicant-1", entryId: null, Entry());
        var reloaded = await sut.GetForWizardAsync(applicationId, "applicant-1");

        Assert.NotNull(reloaded);
        Assert.Equal(2, reloaded!.ResidenceHistoryEntries.Count);
        Assert.Single(db.ResidenceHistoryEntries.Local.Select(e => e.Id).Distinct());
    }
}
