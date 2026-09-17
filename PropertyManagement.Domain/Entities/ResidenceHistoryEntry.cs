namespace PropertyManagement.Domain.Entities;

public class ResidenceHistoryEntry
{
    public int Id { get; set; }

    public int RentalApplicationId { get; set; }
    public RentalApplication? RentalApplication { get; set; }

    public string AddressLine1 { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string ZipCode { get; set; } = string.Empty;
    public string LandlordName { get; set; } = string.Empty;
    public string LandlordPhone { get; set; } = string.Empty;
    public DateOnly MoveInDate { get; set; }
    public DateOnly? MoveOutDate { get; set; }

    /// <summary>Per-row concurrency token: two co-applicants editing different residence rows
    /// never conflict, but a stale save on the same row is rejected (bonus 5a).</summary>
    public int Version { get; set; }
}
