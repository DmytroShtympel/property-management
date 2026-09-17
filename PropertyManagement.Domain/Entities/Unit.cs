using PropertyManagement.Domain.Common;

namespace PropertyManagement.Domain.Entities;

public class Unit : ISoftDeletable
{
    public int Id { get; set; }

    public int PropertyId { get; set; }
    public Property? Property { get; set; }

    public string UnitNumber { get; set; } = string.Empty;
    public int Bedrooms { get; set; }
    public decimal RentAmount { get; set; }

    public int UnitTypeId { get; set; }
    public UnitType? UnitType { get; set; }

    public bool IsRemoved { get; set; }

    public ICollection<RentalApplication> Applications { get; set; } = new List<RentalApplication>();
    public ICollection<Lease> Leases { get; set; } = new List<Lease>();
}
