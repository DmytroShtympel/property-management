namespace PropertyManagement.Domain.Entities;

/// <summary>Join entity linking one or more co-applicants to a RentalApplication (bonus:
/// multi-applicant support). Any user in this collection has full view/edit access to the
/// application, per the "ownership checks apply to all of them" requirement.</summary>
public class ApplicationApplicant
{
    public int Id { get; set; }

    public int RentalApplicationId { get; set; }
    public RentalApplication? RentalApplication { get; set; }

    public string UserId { get; set; } = string.Empty;
    public ApplicationUser? User { get; set; }

    public bool IsPrimary { get; set; }
    public DateTime AddedAtUtc { get; set; } = DateTime.UtcNow;
}
