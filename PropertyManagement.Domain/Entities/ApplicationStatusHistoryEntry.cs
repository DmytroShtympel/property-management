using PropertyManagement.Domain.Enums;

namespace PropertyManagement.Domain.Entities;

public class ApplicationStatusHistoryEntry
{
    public int Id { get; set; }

    public int RentalApplicationId { get; set; }
    public RentalApplication? RentalApplication { get; set; }

    public ApplicationStatus? FromStatus { get; set; }
    public ApplicationStatus ToStatus { get; set; }

    public string? ChangedByUserId { get; set; }
    public ApplicationUser? ChangedByUser { get; set; }
    public DateTime ChangedAtUtc { get; set; } = DateTime.UtcNow;

    public string? Comment { get; set; }
}
