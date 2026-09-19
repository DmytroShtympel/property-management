using PropertyManagement.Domain.Entities;
using PropertyManagement.Domain.Enums;
using PropertyManagement.Infrastructure.Persistence;
using PropertyManagement.Infrastructure.Services;

namespace PropertyManagement.Tests.Infrastructure;

public class ResidenceEntryDuplicateTests
{
    private static async Task<int> SeedDraftAsync(ApplicationDbContext db)
    {
        var applicant = new ApplicationUser { Id = "applicant-1", UserName = "applicant1@demo.local", Email = "applicant1@demo.local" };
        var unitType = new UnitType { Name = "Studio", IsActive = true };
        var property = new Property { OwnerUserId = "manager-1", Name = "Prop", AddressLine1 = "1 St", City = "C", State = "S", ZipCode = "00000" };
        var unit = new Unit { Property = property, UnitType = unitType, UnitNumber = "1A", Bedrooms = 0, RentAmount = 1000m };
        var application = new RentalApplication { Unit = unit, Status = ApplicationStatus.Draft };
        application.Applicants.Add(new ApplicationApplicant { UserId = applicant.Id, IsPrimary = true });
        db.Users.Add(applicant);
        db.RentalApplications.Add(application);
        await db.SaveChangesAsync();
        return application.Id;
    }

    private static ResidenceHistoryInput Entry(string address, DateOnly moveIn) =>
        new(address, "Springfield", "IL", "62701", "Pat Owner", "555-000-1111", moveIn, null, Version: 0);

    [Fact]
    public async Task PostingTheSameNewEntryTwice_KeepsOneRow()
    {
        await using var db = InMemoryDbContextFactory.Create();
        var applicationId = await SeedDraftAsync(db);
        var sut = new ApplicationService(db, userManager: null!, workflow: null!);
        var input = Entry("9 Oak St", new DateOnly(2024, 6, 1));

        var first = await sut.SaveResidenceEntryAsync(applicationId, "applicant-1", entryId: null, input);
        var second = await sut.SaveResidenceEntryAsync(applicationId, "applicant-1", entryId: null, input);

        Assert.Equal(first.Id, second.Id);
        Assert.Single(db.ResidenceHistoryEntries.Where(e => e.RentalApplicationId == applicationId));
    }

    [Fact]
    public async Task SameAddressWithDifferentDates_IsATrueSecondEntry()
    {
        await using var db = InMemoryDbContextFactory.Create();
        var applicationId = await SeedDraftAsync(db);
        var sut = new ApplicationService(db, userManager: null!, workflow: null!);

        await sut.SaveResidenceEntryAsync(applicationId, "applicant-1", entryId: null, Entry("9 Oak St", new DateOnly(2022, 1, 1)));
        await sut.SaveResidenceEntryAsync(applicationId, "applicant-1", entryId: null, Entry("9 Oak St", new DateOnly(2024, 6, 1)));

        Assert.Equal(2, db.ResidenceHistoryEntries.Count(e => e.RentalApplicationId == applicationId));
    }
}
