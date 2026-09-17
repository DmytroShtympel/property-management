using Microsoft.EntityFrameworkCore;
using PropertyManagement.Domain.Entities;
using PropertyManagement.Infrastructure.Persistence;

namespace PropertyManagement.Infrastructure.Services;

public class ListingService(ApplicationDbContext db) : IListingService
{
    // NOTE: mirrors PropertyManagement.Domain.Services.UnitAvailability.IsAvailable, but written
    // as a query expression (rather than calling that method) so EF Core translates the
    // "not currently leased" check into SQL instead of evaluating it in memory.

    public Task<List<Unit>> GetAvailableUnitsAsync(CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return db.Units
            .Include(u => u.UnitType)
            .Include(u => u.Property)
            .Where(u => !u.Leases.Any(l => today >= l.StartDate && today <= l.EndDate))
            .OrderBy(u => u.Property!.Name).ThenBy(u => u.UnitNumber)
            .ToListAsync(ct);
    }

    public Task<Unit?> GetAvailableUnitByIdAsync(int id, CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return db.Units
            .Include(u => u.UnitType)
            .Include(u => u.Property)
            .Where(u => u.Id == id && !u.Leases.Any(l => today >= l.StartDate && today <= l.EndDate))
            .FirstOrDefaultAsync(ct);
    }
}
