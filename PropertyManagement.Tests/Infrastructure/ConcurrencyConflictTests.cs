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

    [Fact]
    public async Task SecondStaleSaveOfTheSameSection_ThrowsConcurrencyException()
    {
        var dbName = Guid.NewGuid().ToString();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(dbName).Options;

        int applicationId;
        await using (var seedDb = new ApplicationDbContext(options))
        {
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
            applicationId = application.Id;

            var service = ServiceOver(seedDb);
            await service.SaveApplicantInformationAsync(applicationId, "applicant-1",
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
