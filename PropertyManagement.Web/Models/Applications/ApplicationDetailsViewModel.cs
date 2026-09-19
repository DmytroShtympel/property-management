using PropertyManagement.Domain.Enums;

namespace PropertyManagement.Web.Models.Applications;

public class ApplicationDetailsViewModel : ApplicationSectionsViewModel
{
    public ApplicationStatus Status { get; set; }
    public string UnitLabel { get; set; } = string.Empty;
    public List<StatusHistoryItemViewModel> StatusHistory { get; set; } = [];
    public LeaseSummaryViewModel? Lease { get; set; }

    public bool CanWithdraw { get; set; }
    public bool CanEdit { get; set; }

    public bool IsManagerView { get; set; }
    public string? ClaimedByName { get; set; }
    public bool ClaimedByCurrentUser { get; set; }
    public List<ApplicationNoteViewModel> Notes { get; set; } = [];

    /// <summary>Editing happens in the wizard; this page shows every section read-only.</summary>
    public override bool IsSectionReadOnly(ApplicationStep section) => true;
}

public class StatusHistoryItemViewModel
{
    public ApplicationStatus? FromStatus { get; set; }
    public ApplicationStatus ToStatus { get; set; }
    public string? ChangedByName { get; set; }
    public DateTime ChangedAtUtc { get; set; }
    public string? Comment { get; set; }
}

public class LeaseSummaryViewModel
{
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public decimal MonthlyRent { get; set; }
}

public class ApplicationNoteViewModel
{
    public string AuthorName { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public string Body { get; set; } = string.Empty;
}
