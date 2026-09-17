using PropertyManagement.Domain.Entities;

namespace PropertyManagement.Infrastructure.Services;

public record UnitInput(string UnitNumber, int Bedrooms, decimal RentAmount, int UnitTypeId);

public class InactiveUnitTypeException(int unitTypeId) : Exception($"Unit type {unitTypeId} is inactive and cannot be selected.");

public class DuplicateUnitNumberException(string unitNumber) : Exception($"Unit number '{unitNumber}' is already used by another unit in this property.");

public interface IUnitService
{
    Task<List<Unit>> GetForPropertyAsync(int propertyId, string managerUserId, CancellationToken ct = default);
    Task<Unit?> GetByIdAsync(int id, string managerUserId, CancellationToken ct = default);
    Task<Unit> CreateAsync(int propertyId, string managerUserId, UnitInput input, CancellationToken ct = default);
    Task<bool> UpdateAsync(int id, string managerUserId, UnitInput input, CancellationToken ct = default);
    Task<bool> SetRemovedAsync(int id, string managerUserId, bool removed, CancellationToken ct = default);
}
