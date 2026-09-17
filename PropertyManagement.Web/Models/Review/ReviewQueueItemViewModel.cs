namespace PropertyManagement.Web.Models.Review;

public class ReviewQueueItemViewModel
{
    public int Id { get; set; }
    public string PropertyName { get; set; } = string.Empty;
    public string UnitNumber { get; set; } = string.Empty;
    public List<string> ApplicantNames { get; set; } = [];
    public DateTime? SubmittedAtUtc { get; set; }
    public string? ClaimedByName { get; set; }
    public bool ClaimedByCurrentUser { get; set; }
}

public class ReviewDecisionViewModel
{
    public int Id { get; set; }
    public string Outcome { get; set; } = string.Empty; // Approve | Deny | Return
    public string? Comment { get; set; }
}
