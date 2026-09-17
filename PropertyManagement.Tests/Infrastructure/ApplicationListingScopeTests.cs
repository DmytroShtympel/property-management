using PropertyManagement.Domain.Entities;
using PropertyManagement.Domain.Enums;
using PropertyManagement.Infrastructure.Persistence;
using PropertyManagement.Infrastructure.Services;

namespace PropertyManagement.Tests.Infrastructure;

public class ApplicationListingScopeTests
{
    private static async Task<(Property property, Unit unit)> SeedPropertyAndUnitAsync(ApplicationDbContext db, string ownerUserId)
    {
        var unitType = new UnitType { Name = "Studio", IsActive = true };
        var property = new Property { OwnerUserId = ownerUserId, Name = "Prop", AddressLine1 = "1 St", City = "C", State = "S", ZipCode = "00000" };
        var unit = new Unit { Property = property, UnitType = unitType, UnitNumber = "1A", Bedrooms = 1, RentAmount = 1000m };
        db.UnitTypes.Add(unitType);
        db.Properties.Add(property);
        db.Units.Add(unit);
        await db.SaveChangesAsync();
        return (property, unit);
    }

    [Fact]
    public async Task ApplicantScope_OnlyReturnsApplicationsTheUserIsOn()
    {
        await using var db = InMemoryDbContextFactory.Create();
        var (_, unit) = await SeedPropertyAndUnitAsync(db, "manager-1");

        var mine = new RentalApplication { Unit = unit, Status = ApplicationStatus.Draft };
        mine.Applicants.Add(new ApplicationApplicant { UserId = "applicant-1", IsPrimary = true });

        var someoneElses = new RentalApplication { Unit = unit, Status = ApplicationStatus.Draft };
        someoneElses.Applicants.Add(new ApplicationApplicant { UserId = "applicant-2", IsPrimary = true });

        db.RentalApplications.AddRange(mine, someoneElses);
        await db.SaveChangesAsync();

        var sut = new ApplicationService(db, userManager: null!, workflow: null!);
        var result = await sut.GetMyApplicationsAsync("applicant-1", status: null, propertyId: null);

        Assert.Single(result);
        Assert.Equal(mine.Id, result[0].Id);
    }

    [Fact]
    public async Task ManagerScope_ReturnsAllApplicationsRegardlessOfPropertyOwner()
    {
        await using var db = InMemoryDbContextFactory.Create();
        var (_, unitOwnedByManager1) = await SeedPropertyAndUnitAsync(db, "manager-1");
        var (_, unitOwnedByManager2) = await SeedPropertyAndUnitAsync(db, "manager-2");

        var appOnManager1Property = new RentalApplication { Unit = unitOwnedByManager1, Status = ApplicationStatus.Submitted };
        appOnManager1Property.Applicants.Add(new ApplicationApplicant { UserId = "applicant-1", IsPrimary = true });

        var appOnManager2Property = new RentalApplication { Unit = unitOwnedByManager2, Status = ApplicationStatus.Submitted };
        appOnManager2Property.Applicants.Add(new ApplicationApplicant { UserId = "applicant-2", IsPrimary = true });

        db.RentalApplications.AddRange(appOnManager1Property, appOnManager2Property);
        await db.SaveChangesAsync();

        var sut = new ApplicationReviewService(db, workflow: null!, claimService: null!);
        var result = await sut.GetAllAsync(status: null, propertyId: null);

        // Managers see every application, not just ones tied to properties they own.
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task StatusFilter_IsAppliedInTheDatabaseQuery()
    {
        await using var db = InMemoryDbContextFactory.Create();
        var (_, unit) = await SeedPropertyAndUnitAsync(db, "manager-1");

        var draft = new RentalApplication { Unit = unit, Status = ApplicationStatus.Draft };
        draft.Applicants.Add(new ApplicationApplicant { UserId = "applicant-1", IsPrimary = true });
        var submitted = new RentalApplication { Unit = unit, Status = ApplicationStatus.Submitted };
        submitted.Applicants.Add(new ApplicationApplicant { UserId = "applicant-1", IsPrimary = true });

        db.RentalApplications.AddRange(draft, submitted);
        await db.SaveChangesAsync();

        var sut = new ApplicationService(db, userManager: null!, workflow: null!);
        var result = await sut.GetMyApplicationsAsync("applicant-1", status: ApplicationStatus.Submitted, propertyId: null);

        Assert.Single(result);
        Assert.Equal(ApplicationStatus.Submitted, result[0].Status);
    }
}
