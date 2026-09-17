using PropertyManagement.Domain.Entities;

namespace PropertyManagement.Domain.Services;

/// <summary>Unit availability is derived, never stored: "A unit whose lease term covers today
/// is not available." A unit stays available to new applicants for as long as no lease on it
/// is currently in term, regardless of how many open applications exist against it.</summary>
public static class UnitAvailability
{
    public static bool IsAvailable(Unit unit, DateOnly today) =>
        unit.Leases.All(lease => today < lease.StartDate || today > lease.EndDate);
}
