using PropertyManagement.Domain.Entities;
using PropertyManagement.Domain.Enums;
using PropertyManagement.Domain.Services;
using PropertyManagement.Infrastructure.Persistence;
using PropertyManagement.Infrastructure.Services;

namespace PropertyManagement.Tests.Infrastructure;

/// <summary>FR-20 (bonus): claiming must actually move Status to Under Review (not just set
/// ClaimedByUserId), and the review queue must still surface a claimed application.</summary>
public class ClaimStatusWiringTests
{
    private static async Task<RentalApplication> SeedSubmittedApplicationAsync(ApplicationDbContext db)
    {
        var unitType = new UnitType { Name = "Studio", IsActive = true };
        var property = new Property { OwnerUserId = "manager-1", Name = "Prop", AddressLine1 = "1 St", City = "C", State = "S", ZipCode = "00000" };
        var unit = new Unit { Property = property, UnitType = unitType, UnitNumber = "1A", Bedrooms = 1, RentAmount = 1000m };
        var application = new RentalApplication { Unit = unit, Status = ApplicationStatus.Submitted, SubmittedAtUtc = DateTime.UtcNow };
        application.Applicants.Add(new ApplicationApplicant { UserId = "maria", IsPrimary = true });

        db.UnitTypes.Add(unitType);
        db.Properties.Add(property);
        db.Units.Add(unit);
        db.RentalApplications.Add(application);
        await db.SaveChangesAsync();
        return application;
    }

    [Fact]
    public async Task ClaimAsync_MovesStatusToUnderReview()
    {
        await using var db = InMemoryDbContextFactory.Create();
        var application = await SeedSubmittedApplicationAsync(db);
        var sut = new ApplicationReviewService(db, new ApplicationWorkflowService(), new ApplicationClaimService());

        await sut.ClaimAsync(application.Id, "manager-1");

        var reloaded = await db.RentalApplications.FindAsync(application.Id);
        Assert.Equal(ApplicationStatus.UnderReview, reloaded!.Status);
        Assert.Equal("manager-1", reloaded.ClaimedByUserId);
    }

    [Fact]
    public async Task ReleaseAsync_MovesStatusBackToSubmitted()
    {
        await using var db = InMemoryDbContextFactory.Create();
        var application = await SeedSubmittedApplicationAsync(db);
        var sut = new ApplicationReviewService(db, new ApplicationWorkflowService(), new ApplicationClaimService());
        await sut.ClaimAsync(application.Id, "manager-1");

        await sut.ReleaseAsync(application.Id, "manager-1");

        var reloaded = await db.RentalApplications.FindAsync(application.Id);
        Assert.Equal(ApplicationStatus.Submitted, reloaded!.Status);
        Assert.Null(reloaded.ClaimedByUserId);
    }

    [Fact]
    public async Task GetQueueAsync_IncludesBothSubmittedAndUnderReviewApplications()
    {
        await using var db = InMemoryDbContextFactory.Create();
        var unclaimed = await SeedSubmittedApplicationAsync(db);
        var claimed = await SeedSubmittedApplicationAsync(db);
        var sut = new ApplicationReviewService(db, new ApplicationWorkflowService(), new ApplicationClaimService());
        await sut.ClaimAsync(claimed.Id, "manager-1");

        var queue = await sut.GetQueueAsync();

        Assert.Equal(2, queue.Count);
        Assert.Contains(queue, a => a.Id == unclaimed.Id && a.Status == ApplicationStatus.Submitted);
        Assert.Contains(queue, a => a.Id == claimed.Id && a.Status == ApplicationStatus.UnderReview);
    }
}
