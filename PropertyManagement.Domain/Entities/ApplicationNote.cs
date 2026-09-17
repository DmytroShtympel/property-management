namespace PropertyManagement.Domain.Entities;

/// <summary>Manager-only note (bonus). Must never be included in any applicant-facing query
/// or view model.</summary>
public class ApplicationNote
{
    public int Id { get; set; }

    public int RentalApplicationId { get; set; }
    public RentalApplication? RentalApplication { get; set; }

    public string AuthorUserId { get; set; } = string.Empty;
    public ApplicationUser? Author { get; set; }

    public string Body { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
