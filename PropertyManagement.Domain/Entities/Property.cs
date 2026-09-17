using PropertyManagement.Domain.Common;

namespace PropertyManagement.Domain.Entities;

public class Property : ISoftDeletable
{
    public int Id { get; set; }

    public string OwnerUserId { get; set; } = string.Empty;
    public ApplicationUser? Owner { get; set; }

    public string Name { get; set; } = string.Empty;
    public string AddressLine1 { get; set; } = string.Empty;
    public string? AddressLine2 { get; set; }
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string ZipCode { get; set; } = string.Empty;

    public bool IsRemoved { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<Unit> Units { get; set; } = new List<Unit>();
}
