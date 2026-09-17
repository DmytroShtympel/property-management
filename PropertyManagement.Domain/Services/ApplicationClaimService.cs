using PropertyManagement.Domain.Entities;
using PropertyManagement.Domain.Enums;
using PropertyManagement.Domain.Exceptions;

namespace PropertyManagement.Domain.Services;

/// <summary>Claim/release for the manager review queue (bonus). Claiming is global across
/// managers (any manager may claim any Submitted application), so this is also where the
/// "already claimed" race is rejected before a decision can be made on it.</summary>
public class ApplicationClaimService
{
    public void Claim(RentalApplication application, string managerUserId, DateTime nowUtc)
    {
        // UnderReview is included so a same-manager re-claim (idempotent) or a different
        // manager's claim attempt on an already-claimed application both reach the ownership
        // check below and get the specific ApplicationAlreadyClaimedException, rather than this
        // generic status error, once Status has moved off Submitted (see ApplicationReviewService).
        if (application.Status is not (ApplicationStatus.Submitted or ApplicationStatus.UnderReview))
        {
            throw new InvalidOperationException("Only a Submitted application can be claimed.");
        }

        if (application.ClaimedByUserId is not null && application.ClaimedByUserId != managerUserId)
        {
            throw new ApplicationAlreadyClaimedException(application.Id, application.ClaimedByUserId);
        }

        application.ClaimedByUserId = managerUserId;
        application.ClaimedAtUtc = nowUtc;
        // ClaimVersion is bumped centrally by ApplicationDbContext.SaveChanges (AD-5) — not here.
    }

    public void Release(RentalApplication application, string managerUserId)
    {
        if (application.ClaimedByUserId != managerUserId)
        {
            throw new ApplicationNotClaimedException(application.Id, "Only the manager who claimed this application can release it.");
        }

        application.ClaimedByUserId = null;
        application.ClaimedAtUtc = null;
        // ClaimVersion is bumped centrally by ApplicationDbContext.SaveChanges (AD-5) — not here.
    }
}
