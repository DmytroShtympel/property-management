using Microsoft.EntityFrameworkCore;
using PropertyManagement.Domain.Entities;
using PropertyManagement.Infrastructure.Persistence;

namespace PropertyManagement.Infrastructure.Services;

public class UnitService(ApplicationDbContext db) : IUnitService
{
    // IgnoreQueryFilters: removed units stay listed on their manager's page, flagged and restorable.
    public Task<List<Unit>> GetForPropertyAsync(int propertyId, string managerUserId, CancellationToken ct = default) =>
        db.Units.IgnoreQueryFilters().Include(u => u.UnitType)
            .Where(u => u.PropertyId == propertyId && u.Property!.OwnerUserId == managerUserId)
            .OrderBy(u => u.UnitNumber)
            .ToListAsync(ct);

    public Task<Unit?> GetByIdAsync(int id, string managerUserId, CancellationToken ct = default) =>
        db.Units.Include(u => u.UnitType).Include(u => u.Property)
            .FirstOrDefaultAsync(u => u.Id == id && u.Property!.OwnerUserId == managerUserId, ct);

    public async Task<Unit> CreateAsync(int propertyId, string managerUserId, UnitInput input, CancellationToken ct = default)
    {
        var property = await db.Properties.FirstOrDefaultAsync(p => p.Id == propertyId && p.OwnerUserId == managerUserId, ct)
            ?? throw new InvalidOperationException("Property not found for this manager.");

        await EnsureUnitTypeSelectableAsync(input.UnitTypeId, currentUnitTypeId: null, ct);
        await EnsureUnitNumberUniqueAsync(property.Id, input.UnitNumber, currentUnitId: null, ct);

        var unit = new Unit
        {
            PropertyId = property.Id,
            UnitNumber = input.UnitNumber,
            Bedrooms = input.Bedrooms,
            RentAmount = input.RentAmount,
            UnitTypeId = input.UnitTypeId
        };

        db.Units.Add(unit);
        await db.SaveChangesAsync(ct);
        return unit;
    }

    public async Task<bool> UpdateAsync(int id, string managerUserId, UnitInput input, CancellationToken ct = default)
    {
        var unit = await GetByIdAsync(id, managerUserId, ct);
        if (unit is null)
        {
            return false;
        }

        await EnsureUnitTypeSelectableAsync(input.UnitTypeId, currentUnitTypeId: unit.UnitTypeId, ct);
        await EnsureUnitNumberUniqueAsync(unit.PropertyId, input.UnitNumber, currentUnitId: unit.Id, ct);

        unit.UnitNumber = input.UnitNumber;
        unit.Bedrooms = input.Bedrooms;
        unit.RentAmount = input.RentAmount;
        unit.UnitTypeId = input.UnitTypeId;

        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> SetRemovedAsync(int id, string managerUserId, bool removed, CancellationToken ct = default)
    {
        var unit = await db.Units.IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Id == id && u.Property!.OwnerUserId == managerUserId, ct);
        if (unit is null)
        {
            return false;
        }

        unit.IsRemoved = removed;
        await db.SaveChangesAsync(ct);
        return true;
    }

    /// <summary>Server-side enforcement: an inactive unit type can keep displaying on a unit
    /// that already uses it, but cannot be newly selected for this or any other unit.</summary>
    private async Task EnsureUnitTypeSelectableAsync(int unitTypeId, int? currentUnitTypeId, CancellationToken ct)
    {
        if (currentUnitTypeId == unitTypeId)
        {
            return;
        }

        var isActive = await db.UnitTypes.Where(t => t.Id == unitTypeId).Select(t => (bool?)t.IsActive).FirstOrDefaultAsync(ct);
        if (isActive != true)
        {
            throw new InactiveUnitTypeException(unitTypeId);
        }
    }

    /// <summary>Unit number is unique within its Property (PRD FR-3) — server-enforced here since
    /// no unique index exists at the database level.</summary>
    private async Task EnsureUnitNumberUniqueAsync(int propertyId, string unitNumber, int? currentUnitId, CancellationToken ct)
    {
        var duplicateExists = await db.Units.AnyAsync(
            u => u.PropertyId == propertyId && u.UnitNumber == unitNumber && u.Id != (currentUnitId ?? 0),
            ct);
        if (duplicateExists)
        {
            throw new DuplicateUnitNumberException(unitNumber);
        }
    }
}
