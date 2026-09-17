using PropertyManagement.Domain.Entities;

namespace PropertyManagement.Domain.Services;

public static class LeaseFactory
{
    public static Lease CreateFromApproval(RentalApplication application, Unit unit, DateTime startDate)
    {
        var start = DateOnly.FromDateTime(startDate);

        return new Lease
        {
            RentalApplicationId = application.Id,
            UnitId = unit.Id,
            StartDate = start,
            EndDate = start.AddMonths(12).AddDays(-1),
            MonthlyRent = unit.RentAmount,
            CreatedAtUtc = startDate
        };
    }
}
