using Microsoft.EntityFrameworkCore;
using PropertyManagement.Domain.Entities;
using PropertyManagement.Domain.Enums;
using PropertyManagement.Infrastructure.Persistence;
using PropertyManagement.Infrastructure.Services;

namespace PropertyManagement.Tests.Infrastructure;

/// <summary>Exercises bonus 5a: "when both save the same section, the second save is rejected
/// as stale." Two separate DbContext instances over the same InMemory database stand in for
/// two concurrent HTTP requests from two co-applicants.</summary>
public class ConcurrencyConflictTests
{
    private static ApplicationService ServiceOver(ApplicationDbContext db) =>
        new(db, userManager: null!, workflow: null!);

    private static async Task<int> SeedDraftAsync(DbContextOptions<ApplicationDbContext> options)
    {
        await using var seedDb = new ApplicationDbContext(options);
        var applicant = new ApplicationUser { Id = "applicant-1", UserName = "applicant1@demo.local", Email = "applicant1@demo.local" };
        seedDb.Users.Add(applicant);

        // RentalApplication.UnitId is a required (non-nullable) FK, so FullGraph()'s
        // Include(a => a.Unit) is a required join — a Unit must exist or the row is
        // silently excluded from every query that goes through it.
        var unitType = new UnitType { Name = "Studio", IsActive = true };
        var property = new Property { OwnerUserId = "manager-1", Name = "Prop", AddressLine1 = "1 St", City = "C", State = "S", ZipCode = "00000" };
        var unit = new Unit { Property = property, UnitType = unitType, UnitNumber = "1A", Bedrooms = 1, RentAmount = 1000m };
        seedDb.UnitTypes.Add(unitType);
        seedDb.Properties.Add(property);
        seedDb.Units.Add(unit);

        var application = new RentalApplication { Unit = unit, Status = ApplicationStatus.Draft };
        application.Applicants.Add(new ApplicationApplicant { UserId = applicant.Id, IsPrimary = true });
        seedDb.RentalApplications.Add(application);
        await seedDb.SaveChangesAsync();
        return application.Id;
    }

    [Fact]
    public async Task SecondFirstSave_FromAFormRenderedBeforeTheRowExisted_IsRejectedAsStale()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var applicationId = await SeedDraftAsync(options);

        // Two co-applicants both open the still-empty section: each form carries Version 0.
        await using var contextA = new ApplicationDbContext(options);
        await using var contextB = new ApplicationDbContext(options);

        await ServiceOver(contextA).SaveApplicantInformationAsync(applicationId, "applicant-1",
            new ApplicantInformationInput("Jordan Lee", "555-1111", "jordan@example.com", "1 Main St", Version: 0));

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() =>
            ServiceOver(contextB).SaveApplicantInformationAsync(applicationId, "applicant-1",
                new ApplicantInformationInput("Casey Kim", "555-2222", "casey@example.com", "2 Oak Ave", Version: 0)));

        await using var verify = new ApplicationDbContext(options);
        Assert.Equal("Jordan Lee", (await verify.ApplicantInformations.SingleAsync()).FullLegalName);
    }

    [Fact]
    public async Task SecondStaleSaveOfTheSameSection_ThrowsConcurrencyException()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var applicationId = await SeedDraftAsync(options);

        await using (var seedDb = new ApplicationDbContext(options))
        {
            await ServiceOver(seedDb).SaveApplicantInformationAsync(applicationId, "applicant-1",
                new ApplicantInformationInput("Jordan Lee", "555-1234", "jordan@example.com", "1 Main St", Version: 0));
        }

        // Two independent "requests" both load the row as it exists after the first save (Version 1).
        await using var contextA = new ApplicationDbContext(options);
        await using var contextB = new ApplicationDbContext(options);

        var infoA = await contextA.ApplicantInformations.FirstAsync(i => i.RentalApplicationId == applicationId);
        var infoB = await contextB.ApplicantInformations.FirstAsync(i => i.RentalApplicationId == applicationId);
        Assert.Equal(infoA.Version, infoB.Version);

        var serviceA = ServiceOver(contextA);
        var serviceB = ServiceOver(contextB);

        // Request A saves first and succeeds.
        await serviceA.SaveApplicantInformationAsync(applicationId, "applicant-1",
            new ApplicantInformationInput("Jordan Lee", "555-9999", "jordan@example.com", "1 Main St", infoA.Version));

        // Request B still has the pre-A version in hand — its save must be rejected as stale.
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() =>
            serviceB.SaveApplicantInformationAsync(applicationId, "applicant-1",
                new ApplicantInformationInput("Jordan Lee", "555-0000", "jordan@example.com", "1 Main St", infoB.Version)));
    }
}
