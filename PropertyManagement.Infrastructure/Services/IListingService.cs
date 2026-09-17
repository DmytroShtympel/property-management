using PropertyManagement.Domain.Entities;

namespace PropertyManagement.Infrastructure.Services;

public interface IListingService
{
    Task<List<Unit>> GetAvailableUnitsAsync(CancellationToken ct = default);
    Task<Unit?> GetAvailableUnitByIdAsync(int id, CancellationToken ct = default);
}
