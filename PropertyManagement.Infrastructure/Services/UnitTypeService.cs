using Microsoft.EntityFrameworkCore;
using PropertyManagement.Domain.Entities;
using PropertyManagement.Infrastructure.Persistence;

namespace PropertyManagement.Infrastructure.Services;

public class UnitTypeService(ApplicationDbContext db) : IUnitTypeService
{
    public Task<List<UnitType>> GetAllAsync(CancellationToken ct = default) =>
        db.UnitTypes.OrderBy(t => t.Name).ToListAsync(ct);

    /// <summary>Active types, plus (when editing an existing unit) whatever type it currently
    /// has even if that type has since gone inactive, so it still renders selected.</summary>
    public Task<List<UnitType>> GetSelectableAsync(int? includeInactiveId = null, CancellationToken ct = default) =>
        db.UnitTypes.Where(t => t.IsActive || t.Id == includeInactiveId)
            .OrderBy(t => t.Name)
            .ToListAsync(ct);

    public Task<UnitType?> GetByIdAsync(int id, CancellationToken ct = default) =>
        db.UnitTypes.FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task<UnitType> CreateAsync(string name, CancellationToken ct = default)
    {
        var type = new UnitType { Name = name, IsActive = true };
        db.UnitTypes.Add(type);
        await db.SaveChangesAsync(ct);
        return type;
    }

    public async Task<bool> UpdateAsync(int id, string name, CancellationToken ct = default)
    {
        var type = await GetByIdAsync(id, ct);
        if (type is null)
        {
            return false;
        }

        type.Name = name;
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> SetActiveAsync(int id, bool isActive, CancellationToken ct = default)
    {
        var type = await GetByIdAsync(id, ct);
        if (type is null)
        {
            return false;
        }

        type.IsActive = isActive;
        await db.SaveChangesAsync(ct);
        return true;
    }
}
