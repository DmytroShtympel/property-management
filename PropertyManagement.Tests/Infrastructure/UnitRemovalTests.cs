using PropertyManagement.Domain.Entities;
using PropertyManagement.Infrastructure.Persistence;
using PropertyManagement.Infrastructure.Services;

namespace PropertyManagement.Tests.Infrastructure;

public class UnitRemovalTests
{
    private static async Task<(Property property, Unit unit)> SeedAsync(ApplicationDbContext db)
    {
        var unitType = new UnitType { Name = "Studio", IsActive = true };
        var property = new Property { OwnerUserId = "manager-1", Name = "Prop", AddressLine1 = "1 St", City = "C", State = "S", ZipCode = "00000" };
        var unit = new Unit { Property = property, UnitType = unitType, UnitNumber = "1A", Bedrooms = 1, RentAmount = 1000m };
        db.Units.Add(unit);
        await db.SaveChangesAsync();
        return (property, unit);
    }

    [Fact]
    public async Task RemovedUnit_StaysListedForItsManager_FlaggedAndRestorable()
    {
        await using var db = InMemoryDbContextFactory.Create();
        var (property, unit) = await SeedAsync(db);
        var sut = new UnitService(db);

        Assert.True(await sut.SetRemovedAsync(unit.Id, "manager-1", removed: true));
        Assert.True(Assert.Single(await sut.GetForPropertyAsync(property.Id, "manager-1")).IsRemoved);

        Assert.True(await sut.SetRemovedAsync(unit.Id, "manager-1", removed: false));
        Assert.False(Assert.Single(await sut.GetForPropertyAsync(property.Id, "manager-1")).IsRemoved);
    }

    [Fact]
    public async Task RemovedUnit_IsNotOfferedToApplicants_UntilRestored()
    {
        await using var db = InMemoryDbContextFactory.Create();
        var (_, unit) = await SeedAsync(db);
        var units = new UnitService(db);
        var listings = new ListingService(db);

        Assert.Single(await listings.GetAvailableUnitsAsync());

        await units.SetRemovedAsync(unit.Id, "manager-1", removed: true);
        Assert.Empty(await listings.GetAvailableUnitsAsync());

        await units.SetRemovedAsync(unit.Id, "manager-1", removed: false);
        Assert.Single(await listings.GetAvailableUnitsAsync());
    }

    [Fact]
    public async Task AnotherManager_CannotListOrRemoveTheUnit()
    {
        await using var db = InMemoryDbContextFactory.Create();
        var (property, unit) = await SeedAsync(db);
        var sut = new UnitService(db);

        Assert.Empty(await sut.GetForPropertyAsync(property.Id, "manager-2"));
        Assert.False(await sut.SetRemovedAsync(unit.Id, "manager-2", removed: true));
    }
}
