using Microsoft.EntityFrameworkCore;
using PropertyManagement.Domain.Entities;
using PropertyManagement.Domain.Enums;
using PropertyManagement.Domain.Services;
using PropertyManagement.Infrastructure.Persistence;
using PropertyManagement.Infrastructure.Services;

namespace PropertyManagement.Tests.Infrastructure;

// Regression test for a real bug: the manager's Applications list showed a co-applicant
// application twice (same property/unit/applicants/status row repeated), even though only one
// RentalApplication row exists in the database. Needs a real relational engine - EF's InMemory
// provider doesn't build real SQL joins, so it can't reproduce a duplicated root result set.
public class ApplicationListDuplicateTests : IAsyncLifetime
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

    [Fact]
    public async Task GetAllAsync_ApplicationWithTwoApplicants_ReturnsItOnce()
    {
        await using var db = CreateContext();

        var applicant1 = new ApplicationUser { Id = "applicant-1", UserName = "casey@demo.local", Email = "casey@demo.local" };
        var applicant2 = new ApplicationUser { Id = "applicant-2", UserName = "jordan@demo.local", Email = "jordan@demo.local" };
        var manager = new ApplicationUser { Id = "manager-1", UserName = "manager1@demo.local", Email = "manager1@demo.local" };
        var unitType = new UnitType { Name = "Studio", IsActive = true };
        var property = new Property { OwnerUserId = manager.Id, Name = "Demo Plaza", AddressLine1 = "100 Demo Way", City = "Springfield", State = "IL", ZipCode = "62701" };
        var unit = new Unit { Property = property, UnitType = unitType, UnitNumber = "A1", Bedrooms = 2, RentAmount = 1800m };
        var application = new RentalApplication { Unit = unit, Status = ApplicationStatus.Approved };
        application.Applicants.Add(new ApplicationApplicant { UserId = applicant1.Id, IsPrimary = true });
        application.Applicants.Add(new ApplicationApplicant { UserId = applicant2.Id, IsPrimary = false });

        db.Users.AddRange(applicant1, applicant2, manager);
        db.RentalApplications.Add(application);
        await db.SaveChangesAsync();

        var sut = new ApplicationReviewService(db, new ApplicationWorkflowService(), new ApplicationClaimService());
        var results = await sut.GetAllAsync(status: null, propertyId: null);

        Assert.Single(results, a => a.Id == application.Id);
    }

    [Fact]
    public async Task GetAllAsync_FilteredByStatus_ApplicationWithTwoApplicants_ReturnsItOnce()
    {
        // The reported symptom happened specifically after picking a status in the filter, so
        // exercise the Where(a => a.Status == status) path too, not just the unfiltered list.
        await using var db = CreateContext();

        var applicant1 = new ApplicationUser { Id = "applicant-1", UserName = "casey@demo.local", Email = "casey@demo.local" };
        var applicant2 = new ApplicationUser { Id = "applicant-2", UserName = "jordan@demo.local", Email = "jordan@demo.local" };
        var manager = new ApplicationUser { Id = "manager-1", UserName = "manager1@demo.local", Email = "manager1@demo.local" };
        var unitType = new UnitType { Name = "Studio", IsActive = true };
        var property = new Property { OwnerUserId = manager.Id, Name = "Demo Plaza", AddressLine1 = "100 Demo Way", City = "Springfield", State = "IL", ZipCode = "62701" };
        var unit = new Unit { Property = property, UnitType = unitType, UnitNumber = "A1", Bedrooms = 2, RentAmount = 1800m };
        var application = new RentalApplication { Unit = unit, Status = ApplicationStatus.Approved };
        application.Applicants.Add(new ApplicationApplicant { UserId = applicant1.Id, IsPrimary = true });
        application.Applicants.Add(new ApplicationApplicant { UserId = applicant2.Id, IsPrimary = false });

        db.Users.AddRange(applicant1, applicant2, manager);
        db.RentalApplications.Add(application);
        await db.SaveChangesAsync();

        var sut = new ApplicationReviewService(db, new ApplicationWorkflowService(), new ApplicationClaimService());
        var results = await sut.GetAllAsync(status: ApplicationStatus.Approved, propertyId: null);

        Assert.Single(results, a => a.Id == application.Id);
    }

    [Fact]
    public async Task GetMyApplicationsAsync_ApplicationWithTwoApplicants_ReturnsItOnce()
    {
        // GetMyApplicationsAsync filters with Where(a => a.Applicants.Any(x => x.UserId ==
        // userId)) on the same FullGraph() that also Includes Applicants - a predicate and an
        // Include on the same one-to-many navigation, which is the shape reported: a co-applicant
        // (Casey Kim + Jordan Lee) application appearing twice in "My Applications".
        await using var db = CreateContext();

        var applicant1 = new ApplicationUser { Id = "applicant-1", UserName = "casey@demo.local", Email = "casey@demo.local" };
        var applicant2 = new ApplicationUser { Id = "applicant-2", UserName = "jordan@demo.local", Email = "jordan@demo.local" };
        var manager = new ApplicationUser { Id = "manager-1", UserName = "manager1@demo.local", Email = "manager1@demo.local" };
        var unitType = new UnitType { Name = "Studio", IsActive = true };
        var property = new Property { OwnerUserId = manager.Id, Name = "Demo Plaza", AddressLine1 = "100 Demo Way", City = "Springfield", State = "IL", ZipCode = "62701" };
        var unit = new Unit { Property = property, UnitType = unitType, UnitNumber = "A1", Bedrooms = 2, RentAmount = 1800m };
        var application = new RentalApplication { Unit = unit, Status = ApplicationStatus.Approved };
        application.Applicants.Add(new ApplicationApplicant { UserId = applicant1.Id, IsPrimary = true });
        application.Applicants.Add(new ApplicationApplicant { UserId = applicant2.Id, IsPrimary = false });

        db.Users.AddRange(applicant1, applicant2, manager);
        db.RentalApplications.Add(application);
        await db.SaveChangesAsync();

        var sut = new ApplicationService(db, userManager: null!, workflow: null!);
        var results = await sut.GetMyApplicationsAsync(applicant1.Id, status: null, propertyId: null);

        Assert.Single(results, a => a.Id == application.Id);
    }
}
