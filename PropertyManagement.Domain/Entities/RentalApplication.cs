using PropertyManagement.Domain.Enums;

namespace PropertyManagement.Domain.Entities;

public class RentalApplication
{
    public int Id { get; set; }

    public int UnitId { get; set; }
    public Unit? Unit { get; set; }

    public ApplicationStatus Status { get; set; } = ApplicationStatus.Draft;
    public ApplicationStep CurrentStep { get; set; } = ApplicationStep.ApplicantInformation;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? SubmittedAtUtc { get; set; }
    public DateTime? DecidedAtUtc { get; set; }

    public string? ClaimedByUserId { get; set; }
    public ApplicationUser? ClaimedByUser { get; set; }
    public DateTime? ClaimedAtUtc { get; set; }

    /// <summary>Concurrency token guarding claim/release races (any manager can claim, so two
    /// simultaneous claims must not both succeed).</summary>
    public int ClaimVersion { get; set; }

    public ICollection<ApplicationApplicant> Applicants { get; set; } = new List<ApplicationApplicant>();
    public ApplicantInformation? ApplicantInformation { get; set; }
    public ICollection<ResidenceHistoryEntry> ResidenceHistoryEntries { get; set; } = new List<ResidenceHistoryEntry>();
    public ICollection<ApplicationStatusHistoryEntry> StatusHistory { get; set; } = new List<ApplicationStatusHistoryEntry>();
    public ICollection<ApplicationNote> Notes { get; set; } = new List<ApplicationNote>();
    public Lease? Lease { get; set; }
}
