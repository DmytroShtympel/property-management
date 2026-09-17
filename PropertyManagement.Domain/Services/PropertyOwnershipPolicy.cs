using PropertyManagement.Domain.Entities;

namespace PropertyManagement.Domain.Services;

/// <summary>Governs Property/Unit CRUD only, which the spec is silent on and the user has
/// decided should be scoped to the owning manager (unlike application review, which is global).</summary>
public static class PropertyOwnershipPolicy
{
    public static bool IsOwner(Property property, string managerUserId) =>
        property.OwnerUserId == managerUserId;
}
