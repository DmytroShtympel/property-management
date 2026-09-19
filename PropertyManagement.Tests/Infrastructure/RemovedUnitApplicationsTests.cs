using PropertyManagement.Domain.Entities;
using PropertyManagement.Domain.Enums;
using PropertyManagement.Infrastructure.Persistence;
using PropertyManagement.Infrastructure.Services;

namespace PropertyManagement.Tests.Infrastructure;

/// <summary>Removing a unit or property is a soft delete that hides it from browsing and
/// management; the confirmation promises "existing applications and leases are kept", so they
/// must stay visible to applicants and managers.</summary>
public class RemovedUnitApplicationsTests
{
    private static async Task<RentalApplication> SeedSubmittedApplicationAsync(ApplicationDbContext db)
    {
        var unitType = new UnitType { Name = "Studio", IsActive = true };
        var property = new Property { OwnerUserId = "manager-1", Name = "Prop", AddressLine1 = "1 St", City = "C", State = "S", ZipCode = "00000" };
        var unit = new Unit { Property = property, UnitType = unitType, UnitNumber = "1A", Bedrooms = 1, RentAmount = 1000m };
        var application = new RentalApplication { Unit = unit, Status = ApplicationStatus.Submitted, SubmittedAtUtc = DateTime.UtcNow };
        application.Applicants.Add(new ApplicationApplicant { UserId = "applicant-1", IsPrimary = true });
        db.RentalApplications.Add(application);
        await db.SaveChangesAsync();
        return application;
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task ApplicationsStayVisible_WhenTheirUnitOrPropertyIsRemoved(bool removeUnit, bool removeProperty)
    {
        await using var db = InMemoryDbContextFactory.Create();
        var application = await SeedSubmittedApplicationAsync(db);
        application.Unit!.IsRemoved = removeUnit;
        application.Unit.Property!.IsRemoved = removeProperty;
        await db.SaveChangesAsync();

        var applicantService = new ApplicationService(db, userManager: null!, workflow: null!);
        var managerService = new ApplicationReviewService(db, workflow: null!, claimService: null!);

        Assert.Single(await applicantService.GetMyApplicationsAsync("applicant-1", status: null, propertyId: null));
        Assert.NotNull(await applicantService.GetDetailsAsync(application.Id, "applicant-1"));
        Assert.Single(await managerService.GetAllAsync(status: null, propertyId: null));
        Assert.Single(await managerService.GetQueueAsync());
        Assert.NotNull(await managerService.GetDetailsAsync(application.Id));
    }
}
