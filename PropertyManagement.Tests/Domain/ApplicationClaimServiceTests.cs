using PropertyManagement.Domain.Entities;
using PropertyManagement.Domain.Enums;
using PropertyManagement.Domain.Exceptions;
using PropertyManagement.Domain.Services;

namespace PropertyManagement.Tests.Domain;

public class ApplicationClaimServiceTests
{
    private readonly ApplicationClaimService _sut = new();

    [Fact]
    public void Claim_WhenUnclaimed_Succeeds()
    {
        var application = new RentalApplication { Id = 1, Status = ApplicationStatus.Submitted };

        _sut.Claim(application, "manager-1", DateTime.UtcNow);

        Assert.Equal("manager-1", application.ClaimedByUserId);
        Assert.NotNull(application.ClaimedAtUtc);
    }

    [Fact]
    public void Claim_WhenAlreadyClaimedByAnotherManager_Throws()
    {
        var application = new RentalApplication { Id = 1, Status = ApplicationStatus.Submitted, ClaimedByUserId = "manager-1" };

        Assert.Throws<ApplicationAlreadyClaimedException>(() => _sut.Claim(application, "manager-2", DateTime.UtcNow));
    }

    [Fact]
    public void Claim_ByTheSameManagerAgain_IsIdempotent()
    {
        var application = new RentalApplication { Id = 1, Status = ApplicationStatus.Submitted, ClaimedByUserId = "manager-1" };

        _sut.Claim(application, "manager-1", DateTime.UtcNow);

        Assert.Equal("manager-1", application.ClaimedByUserId);
    }

    [Fact]
    public void Claim_WhenNotSubmitted_Throws()
    {
        var application = new RentalApplication { Id = 1, Status = ApplicationStatus.Draft };

        Assert.Throws<InvalidOperationException>(() => _sut.Claim(application, "manager-1", DateTime.UtcNow));
    }

    [Fact]
    public void Release_ByClaimant_ClearsClaim()
    {
        var application = new RentalApplication { Id = 1, Status = ApplicationStatus.Submitted, ClaimedByUserId = "manager-1" };

        _sut.Release(application, "manager-1");

        Assert.Null(application.ClaimedByUserId);
    }

    [Fact]
    public void Release_ByNonClaimant_Throws()
    {
        var application = new RentalApplication { Id = 1, Status = ApplicationStatus.Submitted, ClaimedByUserId = "manager-1" };

        Assert.Throws<ApplicationNotClaimedException>(() => _sut.Release(application, "manager-2"));
    }
}
