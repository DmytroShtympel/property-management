namespace PropertyManagement.Domain.Entities;

public class ApplicantInformation
{
    public int Id { get; set; }

    public int RentalApplicationId { get; set; }
    public RentalApplication? RentalApplication { get; set; }

    public string FullLegalName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string CurrentAddress { get; set; } = string.Empty;

    public bool IsComplete { get; set; }

    /// <summary>Concurrency token: rejects a stale save on this section when two co-applicants
    /// edit it at once (bonus 5a).</summary>
    public int Version { get; set; }
}
