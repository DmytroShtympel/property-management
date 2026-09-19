namespace PropertyManagement.Web.Models;

/// <summary>
/// TempData is a plain string-keyed dictionary, so a typo in any one call site silently breaks
/// the flow. These constants give the compiler a chance to catch that.
/// </summary>
public static class TempDataKeys
{
    /// <summary>Set when an optimistic-concurrency conflict rejects a wizard section save.</summary>
    public const string StaleSectionError = "StaleSectionError";

    /// <summary>Set when a review-queue action (claim/approve/reject) fails, including concurrency conflicts.</summary>
    public const string ReviewError = "ReviewError";
}
