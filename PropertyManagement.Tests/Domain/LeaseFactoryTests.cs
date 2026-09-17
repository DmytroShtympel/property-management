using PropertyManagement.Domain.Entities;
using PropertyManagement.Domain.Services;

namespace PropertyManagement.Tests.Domain;

public class LeaseFactoryTests
{
    [Fact]
    public void CreateFromApproval_SetsTwelveMonthTermAndSnapshotsRent()
    {
        var application = new RentalApplication { Id = 1 };
        var unit = new Unit { Id = 2, RentAmount = 2200m };
        var start = new DateTime(2026, 6, 15);

        var lease = LeaseFactory.CreateFromApproval(application, unit, start);

        Assert.Equal(new DateOnly(2026, 6, 15), lease.StartDate);
        Assert.Equal(new DateOnly(2027, 6, 14), lease.EndDate);
        Assert.Equal(2200m, lease.MonthlyRent);
        Assert.Equal(unit.Id, lease.UnitId);
        Assert.Equal(application.Id, lease.RentalApplicationId);
    }

    [Fact]
    public void CreateFromApproval_LaterRentChangeDoesNotAffectExistingLease()
    {
        var application = new RentalApplication { Id = 1 };
        var unit = new Unit { Id = 2, RentAmount = 1000m };

        var lease = LeaseFactory.CreateFromApproval(application, unit, DateTime.UtcNow);
        unit.RentAmount = 5000m;

        Assert.Equal(1000m, lease.MonthlyRent);
    }
}
