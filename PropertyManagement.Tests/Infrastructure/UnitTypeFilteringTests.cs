using PropertyManagement.Domain.Entities;
using PropertyManagement.Infrastructure.Services;

namespace PropertyManagement.Tests.Infrastructure;

public class UnitTypeFilteringTests
{
    [Fact]
    public async Task GetSelectableAsync_ExcludesInactiveTypesByDefault()
    {
        await using var db = InMemoryDbContextFactory.Create();
        db.UnitTypes.AddRange(
            new UnitType { Name = "Studio", IsActive = true },
            new UnitType { Name = "Loft", IsActive = false });
        await db.SaveChangesAsync();

        var sut = new UnitTypeService(db);
        var selectable = await sut.GetSelectableAsync();

        Assert.Single(selectable);
        Assert.Equal("Studio", selectable[0].Name);
    }

    [Fact]
    public async Task GetSelectableAsync_IncludesTheSpecifiedInactiveIdSoAnEditedUnitStillShowsItsCurrentType()
    {
        await using var db = InMemoryDbContextFactory.Create();
        var loft = new UnitType { Name = "Loft", IsActive = false };
        db.UnitTypes.Add(loft);
        await db.SaveChangesAsync();

        var sut = new UnitTypeService(db);
        var selectable = await sut.GetSelectableAsync(includeInactiveId: loft.Id);

        Assert.Contains(selectable, t => t.Id == loft.Id);
    }

    [Fact]
    public async Task CreateUnit_WithInactiveType_Throws()
    {
        await using var db = InMemoryDbContextFactory.Create();
        var property = new Property { OwnerUserId = "manager-1", Name = "Test Property", AddressLine1 = "1 St", City = "C", State = "S", ZipCode = "00000" };
        var inactiveType = new UnitType { Name = "Loft", IsActive = false };
        db.Properties.Add(property);
        db.UnitTypes.Add(inactiveType);
        await db.SaveChangesAsync();

        var sut = new UnitService(db);

        await Assert.ThrowsAsync<InactiveUnitTypeException>(() =>
            sut.CreateAsync(property.Id, "manager-1", new UnitInput("101", 2, 1500m, inactiveType.Id)));
    }

    [Fact]
    public async Task UpdateUnit_KeepingItsExistingInactiveType_DoesNotThrow()
    {
        await using var db = InMemoryDbContextFactory.Create();
        var property = new Property { OwnerUserId = "manager-1", Name = "Test Property", AddressLine1 = "1 St", City = "C", State = "S", ZipCode = "00000" };
        var type = new UnitType { Name = "Loft", IsActive = true };
        db.Properties.Add(property);
        db.UnitTypes.Add(type);
        await db.SaveChangesAsync();

        var sut = new UnitService(db);
        var unit = await sut.CreateAsync(property.Id, "manager-1", new UnitInput("101", 2, 1500m, type.Id));

        type.IsActive = false;
        await db.SaveChangesAsync();

        var updated = await sut.UpdateAsync(unit.Id, "manager-1", new UnitInput("101B", 2, 1600m, type.Id));

        Assert.True(updated);
    }

    [Fact]
    public async Task CreateUnit_WithUnitNumberAlreadyUsedInTheSameProperty_Throws()
    {
        await using var db = InMemoryDbContextFactory.Create();
        var property = new Property { OwnerUserId = "manager-1", Name = "Test Property", AddressLine1 = "1 St", City = "C", State = "S", ZipCode = "00000" };
        var type = new UnitType { Name = "Studio", IsActive = true };
        db.Properties.Add(property);
        db.UnitTypes.Add(type);
        await db.SaveChangesAsync();

        var sut = new UnitService(db);
        await sut.CreateAsync(property.Id, "manager-1", new UnitInput("101", 2, 1500m, type.Id));

        await Assert.ThrowsAsync<DuplicateUnitNumberException>(() =>
            sut.CreateAsync(property.Id, "manager-1", new UnitInput("101", 1, 1200m, type.Id)));
    }

    [Fact]
    public async Task UpdateUnit_KeepingItsOwnUnitNumber_DoesNotThrow()
    {
        await using var db = InMemoryDbContextFactory.Create();
        var property = new Property { OwnerUserId = "manager-1", Name = "Test Property", AddressLine1 = "1 St", City = "C", State = "S", ZipCode = "00000" };
        var type = new UnitType { Name = "Studio", IsActive = true };
        db.Properties.Add(property);
        db.UnitTypes.Add(type);
        await db.SaveChangesAsync();

        var sut = new UnitService(db);
        var unit = await sut.CreateAsync(property.Id, "manager-1", new UnitInput("101", 2, 1500m, type.Id));

        var updated = await sut.UpdateAsync(unit.Id, "manager-1", new UnitInput("101", 3, 1600m, type.Id));

        Assert.True(updated);
    }
}
