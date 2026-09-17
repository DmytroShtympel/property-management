using Microsoft.EntityFrameworkCore;
using PropertyManagement.Domain.Entities;
using PropertyManagement.Infrastructure.Persistence;

namespace PropertyManagement.Infrastructure.Services;

public class PropertyService(ApplicationDbContext db) : IPropertyService
{
    public Task<List<Property>> GetAllActiveAsync(CancellationToken ct = default) =>
        db.Properties.OrderBy(p => p.Name).ToListAsync(ct);

    public async Task<List<Property>> GetOwnedAsync(string managerUserId, bool includeRemoved = false, CancellationToken ct = default)
    {
        var query = db.Properties.Include(p => p.Units).Where(p => p.OwnerUserId == managerUserId);

        if (includeRemoved)
        {
            query = query.IgnoreQueryFilters().Where(p => p.OwnerUserId == managerUserId);
        }

        return await query.OrderBy(p => p.Name).ToListAsync(ct);
    }

    public Task<Property?> GetByIdAsync(int id, string managerUserId, CancellationToken ct = default) =>
        db.Properties.Include(p => p.Units)
            .FirstOrDefaultAsync(p => p.Id == id && p.OwnerUserId == managerUserId, ct);

    public async Task<Property> CreateAsync(string managerUserId, PropertyInput input, CancellationToken ct = default)
    {
        var property = new Property
        {
            OwnerUserId = managerUserId,
            Name = input.Name,
            AddressLine1 = input.AddressLine1,
            AddressLine2 = input.AddressLine2,
            City = input.City,
            State = input.State,
            ZipCode = input.ZipCode
        };

        db.Properties.Add(property);
        await db.SaveChangesAsync(ct);
        return property;
    }

    public async Task<bool> UpdateAsync(int id, string managerUserId, PropertyInput input, CancellationToken ct = default)
    {
        var property = await GetByIdAsync(id, managerUserId, ct);
        if (property is null)
        {
            return false;
        }

        property.Name = input.Name;
        property.AddressLine1 = input.AddressLine1;
        property.AddressLine2 = input.AddressLine2;
        property.City = input.City;
        property.State = input.State;
        property.ZipCode = input.ZipCode;

        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> SetRemovedAsync(int id, string managerUserId, bool removed, CancellationToken ct = default)
    {
        var property = await db.Properties.IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.Id == id && p.OwnerUserId == managerUserId, ct);
        if (property is null)
        {
            return false;
        }

        property.IsRemoved = removed;
        await db.SaveChangesAsync(ct);
        return true;
    }
}
