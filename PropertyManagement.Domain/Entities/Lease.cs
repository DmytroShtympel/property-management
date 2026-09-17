namespace PropertyManagement.Domain.Entities;

public class Lease
{
    public int Id { get; set; }

    public int RentalApplicationId { get; set; }
    public RentalApplication? RentalApplication { get; set; }

    public int UnitId { get; set; }
    public Unit? Unit { get; set; }

    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }

    /// <summary>Rent snapshotted from Unit.RentAmount at approval time, so later rent changes
    /// on the unit don't retroactively alter an issued lease.</summary>
    public decimal MonthlyRent { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
