namespace PropertyManagement.Domain.Enums;

public enum ApplicationStatus
{
    Draft = 0,
    Submitted = 1,
    Returned = 2,
    Approved = 3,
    Denied = 4,
    Withdrawn = 5,

    /// <summary>Bonus FR-20: a Property Manager has claimed this Submitted application for
    /// review. Reversible back to Submitted via Release; not terminal.</summary>
    [System.ComponentModel.DataAnnotations.Display(Name = "Under Review")]
    UnderReview = 6
}
