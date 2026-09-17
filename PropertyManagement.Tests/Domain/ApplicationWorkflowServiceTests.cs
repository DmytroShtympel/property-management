using PropertyManagement.Domain.Entities;
using PropertyManagement.Domain.Enums;
using PropertyManagement.Domain.Exceptions;
using PropertyManagement.Domain.Services;

namespace PropertyManagement.Tests.Domain;

public class ApplicationWorkflowServiceTests
{
    private readonly ApplicationWorkflowService _sut = new();

    private static RentalApplication CompleteDraftApplication() => new()
    {
        Id = 1,
        Status = ApplicationStatus.Draft,
        ApplicantInformation = new ApplicantInformation
        {
            FullLegalName = "Jordan Lee",
            PhoneNumber = "555-123-4567",
            Email = "jordan@example.com",
            CurrentAddress = "1 Main St",
            IsComplete = true
        },
        ResidenceHistoryEntries =
        [
            new ResidenceHistoryEntry
            {
                AddressLine1 = "1 Main St",
                City = "Springfield",
                State = "IL",
                ZipCode = "62701",
                LandlordName = "Pat Owner",
                LandlordPhone = "555-000-0000",
                MoveInDate = new DateOnly(2020, 1, 1)
            }
        ]
    };

    private static Unit AvailableUnit(int id = 5) => new() { Id = id, RentAmount = 1500 };

    [Fact]
    public void Submit_WithCompleteSections_TransitionsToSubmittedAndRecordsHistory()
    {
        var application = CompleteDraftApplication();
        var now = DateTime.UtcNow;

        _sut.Submit(application, AvailableUnit(), "applicant-1", now);

        Assert.Equal(ApplicationStatus.Submitted, application.Status);
        Assert.Equal(now, application.SubmittedAtUtc);
        Assert.Single(application.StatusHistory);
        Assert.Equal(ApplicationStatus.Draft, application.StatusHistory.Single().FromStatus);
        Assert.Equal(ApplicationStatus.Submitted, application.StatusHistory.Single().ToStatus);
    }

    [Fact]
    public void Submit_WithIncompleteApplicantInformation_Throws()
    {
        var application = CompleteDraftApplication();
        application.ApplicantInformation!.IsComplete = false;

        Assert.Throws<InvalidOperationException>(() => _sut.Submit(application, AvailableUnit(), "applicant-1", DateTime.UtcNow));
    }

    [Fact]
    public void Submit_WithNoResidenceHistory_Throws()
    {
        var application = CompleteDraftApplication();
        application.ResidenceHistoryEntries.Clear();

        Assert.Throws<InvalidOperationException>(() => _sut.Submit(application, AvailableUnit(), "applicant-1", DateTime.UtcNow));
    }

    [Fact]
    public void Submit_WhenUnitHasActiveLease_ThrowsUnitNotAvailable()
    {
        var application = CompleteDraftApplication();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var leasedUnit = new Unit
        {
            Id = 5,
            RentAmount = 1500,
            Leases = [new Lease { StartDate = today.AddDays(-1), EndDate = today.AddMonths(12) }]
        };

        Assert.Throws<UnitNotAvailableException>(() => _sut.Submit(application, leasedUnit, "applicant-1", DateTime.UtcNow));
        Assert.Equal(ApplicationStatus.Draft, application.Status);
    }

    [Fact]
    public void Approve_WhenUnitWasLeasedByACompetingApplication_ThrowsUnitNotAvailable()
    {
        // Two applications for the same unit, each independently claimed and legal to approve —
        // this is the "second Approve finds the unit already leased" guard (adversarial review Finding 1).
        var application = new RentalApplication { Id = 2, Status = ApplicationStatus.Submitted, ClaimedByUserId = "manager-2" };
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var unit = new Unit
        {
            Id = 5,
            RentAmount = 1500,
            Leases = [new Lease { StartDate = today, EndDate = today.AddMonths(12) }]
        };

        Assert.Throws<UnitNotAvailableException>(() => _sut.Approve(application, unit, "manager-2", DateTime.UtcNow));
        Assert.Equal(ApplicationStatus.Submitted, application.Status);
    }

    [Theory]
    [InlineData(ApplicationStatus.Approved)]
    [InlineData(ApplicationStatus.Denied)]
    [InlineData(ApplicationStatus.Withdrawn)]
    public void TerminalStatuses_RejectAnyFurtherTransition(ApplicationStatus terminalStatus)
    {
        var application = new RentalApplication { Id = 1, Status = terminalStatus };

        Assert.Throws<InvalidApplicationTransitionException>(() => _sut.Withdraw(application, "applicant-1", DateTime.UtcNow));
    }

    [Fact]
    public void Submit_FromApproved_IsRejectedAsInvalidTransition()
    {
        var application = new RentalApplication { Id = 1, Status = ApplicationStatus.Approved };

        Assert.Throws<InvalidApplicationTransitionException>(() => _sut.Submit(application, AvailableUnit(), "applicant-1", DateTime.UtcNow));
    }

    [Fact]
    public void Approve_WithoutClaim_Throws()
    {
        var application = new RentalApplication { Id = 1, Status = ApplicationStatus.Submitted, ClaimedByUserId = null };
        var unit = new Unit { Id = 5, RentAmount = 1500 };

        Assert.Throws<ApplicationNotClaimedException>(() => _sut.Approve(application, unit, "manager-1", DateTime.UtcNow));
    }

    [Fact]
    public void Approve_WhenClaimedByAnotherManager_Throws()
    {
        var application = new RentalApplication { Id = 1, Status = ApplicationStatus.Submitted, ClaimedByUserId = "manager-2" };
        var unit = new Unit { Id = 5, RentAmount = 1500 };

        Assert.Throws<ApplicationNotClaimedException>(() => _sut.Approve(application, unit, "manager-1", DateTime.UtcNow));
    }

    [Fact]
    public void Approve_WhenClaimedByCaller_CreatesLeaseAndClearsClaim()
    {
        var application = new RentalApplication { Id = 1, Status = ApplicationStatus.Submitted, ClaimedByUserId = "manager-1" };
        var unit = new Unit { Id = 5, RentAmount = 1500 };
        var decisionDate = new DateTime(2026, 3, 1);

        var lease = _sut.Approve(application, unit, "manager-1", decisionDate);

        Assert.Equal(ApplicationStatus.Approved, application.Status);
        Assert.Null(application.ClaimedByUserId);
        Assert.Same(lease, application.Lease);
        Assert.Equal(new DateOnly(2026, 3, 1), lease.StartDate);
        Assert.Equal(new DateOnly(2027, 2, 28), lease.EndDate);
    }

    [Fact]
    public void Deny_WithoutComment_Throws()
    {
        var application = new RentalApplication { Id = 1, Status = ApplicationStatus.Submitted, ClaimedByUserId = "manager-1" };

        Assert.Throws<ArgumentException>(() => _sut.Deny(application, "manager-1", "", DateTime.UtcNow));
    }

    [Fact]
    public void Return_ThenResubmit_IsAllowedBecauseReturnedIsNotTerminal()
    {
        var application = CompleteDraftApplication();
        application.Status = ApplicationStatus.Submitted;
        application.ClaimedByUserId = "manager-1";

        _sut.Return(application, "manager-1", "Please clarify employer.", DateTime.UtcNow);
        Assert.Equal(ApplicationStatus.Returned, application.Status);

        _sut.Submit(application, AvailableUnit(), "applicant-1", DateTime.UtcNow);
        Assert.Equal(ApplicationStatus.Submitted, application.Status);
    }

    [Fact]
    public void Withdraw_FromDraft_Succeeds()
    {
        var application = new RentalApplication { Id = 1, Status = ApplicationStatus.Draft };

        _sut.Withdraw(application, "applicant-1", DateTime.UtcNow);

        Assert.Equal(ApplicationStatus.Withdrawn, application.Status);
    }

    [Fact]
    public void MarkUnderReview_FromSubmitted_TransitionsAndRecordsHistory()
    {
        var application = new RentalApplication { Id = 1, Status = ApplicationStatus.Submitted };
        var now = DateTime.UtcNow;

        _sut.MarkUnderReview(application, "manager-1", now);

        Assert.Equal(ApplicationStatus.UnderReview, application.Status);
        Assert.Single(application.StatusHistory);
        Assert.Equal(ApplicationStatus.Submitted, application.StatusHistory.Single().FromStatus);
        Assert.Equal(ApplicationStatus.UnderReview, application.StatusHistory.Single().ToStatus);
    }

    [Fact]
    public void MarkUnderReview_WhenAlreadyUnderReview_IsIdempotentAndRecordsNoExtraHistory()
    {
        var application = new RentalApplication { Id = 1, Status = ApplicationStatus.UnderReview };

        _sut.MarkUnderReview(application, "manager-1", DateTime.UtcNow);

        Assert.Equal(ApplicationStatus.UnderReview, application.Status);
        Assert.Empty(application.StatusHistory);
    }

    [Fact]
    public void ReturnToQueue_FromUnderReview_TransitionsBackToSubmitted()
    {
        var application = new RentalApplication { Id = 1, Status = ApplicationStatus.UnderReview };

        _sut.ReturnToQueue(application, "manager-1", DateTime.UtcNow);

        Assert.Equal(ApplicationStatus.Submitted, application.Status);
        Assert.Single(application.StatusHistory);
    }

    [Fact]
    public void Approve_FromUnderReview_Succeeds()
    {
        var application = new RentalApplication { Id = 1, Status = ApplicationStatus.UnderReview, ClaimedByUserId = "manager-1" };
        var unit = new Unit { Id = 5, RentAmount = 1500 };

        var lease = _sut.Approve(application, unit, "manager-1", DateTime.UtcNow);

        Assert.Equal(ApplicationStatus.Approved, application.Status);
        Assert.Same(lease, application.Lease);
    }
}
