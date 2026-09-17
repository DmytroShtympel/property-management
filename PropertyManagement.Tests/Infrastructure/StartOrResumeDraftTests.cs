using PropertyManagement.Domain.Entities;
using PropertyManagement.Domain.Enums;
using PropertyManagement.Infrastructure.Persistence;
using PropertyManagement.Infrastructure.Services;

namespace PropertyManagement.Tests.Infrastructure;

public class StartOrResumeDraftTests
{
    private static async Task<Unit> SeedUnitAsync(ApplicationDbContext db)
    {
        var unitType = new UnitType { Name = "Studio", IsActive = true };
        var property = new Property { OwnerUserId = "manager-1", Name = "Prop", AddressLine1 = "1 St", City = "C", State = "S", ZipCode = "00000" };
        var unit = new Unit { Property = property, UnitType = unitType, UnitNumber = "4B", Bedrooms = 1, RentAmount = 1000m };
        db.UnitTypes.Add(unitType);
        db.Properties.Add(property);
        db.Units.Add(unit);
        await db.SaveChangesAsync();
        return unit;
    }

    [Theory]
    [InlineData(ApplicationStatus.Draft)]
    [InlineData(ApplicationStatus.Returned)]
    [InlineData(ApplicationStatus.Submitted)]
    public async Task StartOrResumeDraftAsync_WithAnyOpenApplication_ReturnsTheExistingOneInsteadOfCreatingADuplicate(ApplicationStatus openStatus)
    {
        await using var db = InMemoryDbContextFactory.Create();
        var unit = await SeedUnitAsync(db);

        var existing = new RentalApplication { Unit = unit, Status = openStatus };
        existing.Applicants.Add(new ApplicationApplicant { UserId = "maria", IsPrimary = true });
        db.RentalApplications.Add(existing);
        await db.SaveChangesAsync();

        var sut = new ApplicationService(db, userManager: null!, workflow: null!);
        var result = await sut.StartOrResumeDraftAsync(unit.Id, "maria");

        Assert.Equal(existing.Id, result.Id);
        Assert.Single(db.RentalApplications);
    }

    [Theory]
    [InlineData(ApplicationStatus.Approved)]
    [InlineData(ApplicationStatus.Denied)]
    [InlineData(ApplicationStatus.Withdrawn)]
    public async Task StartOrResumeDraftAsync_WithOnlyATerminalApplication_CreatesANewDraft(ApplicationStatus terminalStatus)
    {
        await using var db = InMemoryDbContextFactory.Create();
        var unit = await SeedUnitAsync(db);

        var terminal = new RentalApplication { Unit = unit, Status = terminalStatus };
        terminal.Applicants.Add(new ApplicationApplicant { UserId = "maria", IsPrimary = true });
        db.RentalApplications.Add(terminal);
        await db.SaveChangesAsync();

        var sut = new ApplicationService(db, userManager: null!, workflow: null!);
        var result = await sut.StartOrResumeDraftAsync(unit.Id, "maria");

        Assert.NotEqual(terminal.Id, result.Id);
        Assert.Equal(2, db.RentalApplications.Count());
    }
}
