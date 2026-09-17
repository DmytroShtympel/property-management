using PropertyManagement.Domain.Entities;

namespace PropertyManagement.Infrastructure.Services;

public interface IUnitTypeService
{
    Task<List<UnitType>> GetAllAsync(CancellationToken ct = default);
    Task<List<UnitType>> GetSelectableAsync(int? includeInactiveId = null, CancellationToken ct = default);
    Task<UnitType?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<UnitType> CreateAsync(string name, CancellationToken ct = default);
    Task<bool> UpdateAsync(int id, string name, CancellationToken ct = default);
    Task<bool> SetActiveAsync(int id, bool isActive, CancellationToken ct = default);
}
