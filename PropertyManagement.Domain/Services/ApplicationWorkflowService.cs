using PropertyManagement.Domain.Entities;
using PropertyManagement.Domain.Enums;
using PropertyManagement.Domain.Exceptions;

namespace PropertyManagement.Domain.Services;

/// <summary>Owns the RentalApplication status state machine. Every state-changing method
/// appends an ApplicationStatusHistoryEntry itself, so callers never need to build history
/// entries by hand, and invalid transitions are rejected here (not just in the UI) so the
/// invariant can't be bypassed by a direct POST.</summary>
public class ApplicationWorkflowService
{
    private static readonly Dictionary<ApplicationStatus, ApplicationStatus[]> LegalTransitions = new()
    {
        [ApplicationStatus.Draft] = [ApplicationStatus.Submitted, ApplicationStatus.Withdrawn],
        // UnderReview is reachable from Submitted (Claim) and reverses back to Submitted
        // (Release). Submitted itself still allows the outcome transitions directly, since
        // claim-gating is enforced by RequireClaimedBy, not by this status machine (a direct
        // Submitted->Approved call correctly fails on the claim check, not a transition error).
        [ApplicationStatus.Submitted] = [ApplicationStatus.UnderReview, ApplicationStatus.Approved, ApplicationStatus.Denied, ApplicationStatus.Returned, ApplicationStatus.Withdrawn],
        [ApplicationStatus.UnderReview] = [ApplicationStatus.Submitted, ApplicationStatus.Approved, ApplicationStatus.Denied, ApplicationStatus.Returned, ApplicationStatus.Withdrawn],
        [ApplicationStatus.Returned] = [ApplicationStatus.Submitted, ApplicationStatus.Withdrawn],
        [ApplicationStatus.Approved] = [],
        [ApplicationStatus.Denied] = [],
        [ApplicationStatus.Withdrawn] = []
    };

    public static bool IsTerminal(ApplicationStatus status) =>
        LegalTransitions.TryGetValue(status, out var next) && next.Length == 0;

    private static void GuardTransition(RentalApplication application, ApplicationStatus to)
    {
        var allowed = LegalTransitions.TryGetValue(application.Status, out var next) && next.Contains(to);
        if (!allowed)
        {
            throw new InvalidApplicationTransitionException(application.Status, to);
        }
    }

    private static void AppendHistory(RentalApplication application, ApplicationStatus to, string? changedByUserId, string? comment, DateTime nowUtc)
    {
        application.StatusHistory.Add(new ApplicationStatusHistoryEntry
        {
            RentalApplicationId = application.Id,
            FromStatus = application.Status,
            ToStatus = to,
            ChangedByUserId = changedByUserId,
            ChangedAtUtc = nowUtc,
            Comment = comment
        });
        application.Status = to;
    }

    /// <summary>Server-authoritative re-validation that every section is actually complete,
    /// independent of client-side gating, so Submit can't be reached with an incomplete
    /// Applicant Information section or zero residence history. Also re-checks unit
    /// availability (a sibling application on the same unit may have been approved since this
    /// one was drafted) so a second application can't be submitted against an already-leased
    /// unit.</summary>
    public void Submit(RentalApplication application, Unit unit, string applicantUserId, DateTime nowUtc)
    {
        GuardTransition(application, ApplicationStatus.Submitted);

        if (!UnitAvailability.IsAvailable(unit, DateOnly.FromDateTime(nowUtc)))
        {
            throw new UnitNotAvailableException(unit.Id);
        }

        if (application.ApplicantInformation is not { IsComplete: true })
        {
            throw new InvalidOperationException("Applicant Information must be complete before the application can be submitted.");
        }

        if (application.ResidenceHistoryEntries.Count == 0)
        {
            throw new InvalidOperationException("At least one Residence History entry is required before the application can be submitted.");
        }

        application.SubmittedAtUtc = nowUtc;
        AppendHistory(application, ApplicationStatus.Submitted, applicantUserId, comment: null, nowUtc);
    }

    /// <summary>Re-checks unit availability at approval time (not just at Submit) so that of two
    /// applications competing for the same unit, only the first Approve can succeed — the second
    /// finds the unit already leased even though its own claim/transition checks pass.</summary>
    public Lease Approve(RentalApplication application, Unit unit, string reviewerUserId, DateTime decisionDate)
    {
        GuardTransition(application, ApplicationStatus.Approved);
        RequireClaimedBy(application, reviewerUserId);

        if (!UnitAvailability.IsAvailable(unit, DateOnly.FromDateTime(decisionDate)))
        {
            throw new UnitNotAvailableException(unit.Id);
        }

        var lease = LeaseFactory.CreateFromApproval(application, unit, decisionDate);
        application.Lease = lease;
        application.DecidedAtUtc = decisionDate;
        ClearClaim(application);
        AppendHistory(application, ApplicationStatus.Approved, reviewerUserId, comment: null, decisionDate);

        return lease;
    }

    public void Deny(RentalApplication application, string reviewerUserId, string comment, DateTime nowUtc)
    {
        RequireComment(comment, nameof(Deny));
        GuardTransition(application, ApplicationStatus.Denied);
        RequireClaimedBy(application, reviewerUserId);

        application.DecidedAtUtc = nowUtc;
        ClearClaim(application);
        AppendHistory(application, ApplicationStatus.Denied, reviewerUserId, comment, nowUtc);
    }

    public void Return(RentalApplication application, string reviewerUserId, string comment, DateTime nowUtc)
    {
        RequireComment(comment, nameof(Return));
        GuardTransition(application, ApplicationStatus.Returned);
        RequireClaimedBy(application, reviewerUserId);

        ClearClaim(application);
        AppendHistory(application, ApplicationStatus.Returned, reviewerUserId, comment, nowUtc);
    }

    public void Withdraw(RentalApplication application, string applicantUserId, DateTime nowUtc)
    {
        GuardTransition(application, ApplicationStatus.Withdrawn);
        ClearClaim(application);
        AppendHistory(application, ApplicationStatus.Withdrawn, applicantUserId, comment: null, nowUtc);
    }

    /// <summary>Bonus FR-20: claiming moves a Submitted application to Under Review so it is
    /// distinguishable from an unclaimed one by status alone (filterable via FR-15/FR-19), not
    /// just by ClaimedByUserId. A same-manager re-claim is idempotent and appends no history.</summary>
    public void MarkUnderReview(RentalApplication application, string managerUserId, DateTime nowUtc)
    {
        if (application.Status == ApplicationStatus.UnderReview)
        {
            return;
        }

        GuardTransition(application, ApplicationStatus.UnderReview);
        AppendHistory(application, ApplicationStatus.UnderReview, managerUserId, comment: null, nowUtc);
    }

    /// <summary>Bonus FR-20: releasing a claim returns the application to the visible queue.</summary>
    public void ReturnToQueue(RentalApplication application, string managerUserId, DateTime nowUtc)
    {
        GuardTransition(application, ApplicationStatus.Submitted);
        AppendHistory(application, ApplicationStatus.Submitted, managerUserId, comment: null, nowUtc);
    }

    private static void RequireComment(string? comment, string action)
    {
        if (string.IsNullOrWhiteSpace(comment))
        {
            throw new ArgumentException($"A comment is required to {action.ToLowerInvariant()} an application.", nameof(comment));
        }
    }

    private static void RequireClaimedBy(RentalApplication application, string reviewerUserId)
    {
        if (application.ClaimedByUserId != reviewerUserId)
        {
            throw new ApplicationNotClaimedException(application.Id, "The application must be claimed by this manager before it can be decided.");
        }
    }

    private static void ClearClaim(RentalApplication application)
    {
        application.ClaimedByUserId = null;
        application.ClaimedAtUtc = null;
        // ClaimVersion is bumped centrally by ApplicationDbContext.SaveChanges (AD-5) — not here.
    }
}
