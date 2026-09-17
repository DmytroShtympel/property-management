using PropertyManagement.Domain.Entities;
using PropertyManagement.Domain.Services;

namespace PropertyManagement.Tests.Domain;

public class UnitAvailabilityTests
{
    private static readonly DateOnly Today = new(2026, 6, 15);

    [Fact]
    public void IsAvailable_WithNoLeases_IsTrue()
    {
        var unit = new Unit { Id = 1 };

        Assert.True(UnitAvailability.IsAvailable(unit, Today));
    }

    [Fact]
    public void IsAvailable_WithLeaseCoveringToday_IsFalse()
    {
        var unit = new Unit { Id = 1 };
        unit.Leases.Add(new Lease { StartDate = new DateOnly(2026, 1, 1), EndDate = new DateOnly(2026, 12, 31) });

        Assert.False(UnitAvailability.IsAvailable(unit, Today));
    }

    [Fact]
    public void IsAvailable_WithLeaseEndedYesterday_IsTrue()
    {
        var unit = new Unit { Id = 1 };
        unit.Leases.Add(new Lease { StartDate = new DateOnly(2025, 1, 1), EndDate = Today.AddDays(-1) });

        Assert.True(UnitAvailability.IsAvailable(unit, Today));
    }

    [Fact]
    public void IsAvailable_WithLeaseStartingTomorrow_IsTrue()
    {
        var unit = new Unit { Id = 1 };
        unit.Leases.Add(new Lease { StartDate = Today.AddDays(1), EndDate = Today.AddYears(1) });

        Assert.True(UnitAvailability.IsAvailable(unit, Today));
    }

    [Fact]
    public void IsAvailable_OnExactStartOrEndDate_IsFalse()
    {
        var unit = new Unit { Id = 1 };
        unit.Leases.Add(new Lease { StartDate = Today, EndDate = Today });

        Assert.False(UnitAvailability.IsAvailable(unit, Today));
    }
}
