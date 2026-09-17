using PropertyManagement.Domain.Entities;

namespace PropertyManagement.Infrastructure.Services;

public record PropertyInput(string Name, string AddressLine1, string? AddressLine2, string City, string State, string ZipCode);

public interface IPropertyService
{
    /// <summary>All non-removed properties system-wide, regardless of owner — used only to
    /// populate the property filter dropdown on the (global) application listing/review
    /// screens, not for any CRUD or ownership check.</summary>
    Task<List<Property>> GetAllActiveAsync(CancellationToken ct = default);
    Task<List<Property>> GetOwnedAsync(string managerUserId, bool includeRemoved = false, CancellationToken ct = default);
    Task<Property?> GetByIdAsync(int id, string managerUserId, CancellationToken ct = default);
    Task<Property> CreateAsync(string managerUserId, PropertyInput input, CancellationToken ct = default);
    Task<bool> UpdateAsync(int id, string managerUserId, PropertyInput input, CancellationToken ct = default);
    Task<bool> SetRemovedAsync(int id, string managerUserId, bool removed, CancellationToken ct = default);
}
