using PropertyManagement.Domain.Entities;
using PropertyManagement.Domain.Enums;
using PropertyManagement.Infrastructure.Persistence;
using PropertyManagement.Infrastructure.Services;

namespace PropertyManagement.Tests.Infrastructure;

/// <summary>FR-9: a Section action must be rejected server-side when the Application is not
/// Draft/Returned, even if the client still renders an edit form. Exercised through
/// SaveApplicantInformationAsync (LoadEditableAsync's status gate is shared by every editing
/// method), with both the allowed and rejected path per FR-17's coverage mandate.</summary>
public class EditableGuardTests
{
    private static async Task<RentalApplication> SeedApplicationAsync(ApplicationDbContext db, ApplicationStatus status)
    {
        var unitType = new UnitType { Name = "Studio", IsActive = true };
        var property = new Property { OwnerUserId = "manager-1", Name = "Prop", AddressLine1 = "1 St", City = "C", State = "S", ZipCode = "00000" };
        var unit = new Unit { Property = property, UnitType = unitType, UnitNumber = "1A", Bedrooms = 1, RentAmount = 1000m };
        var application = new RentalApplication { Unit = unit, Status = status };
        application.Applicants.Add(new ApplicationApplicant { UserId = "maria", IsPrimary = true });

        db.UnitTypes.Add(unitType);
        db.Properties.Add(property);
        db.Units.Add(unit);
        db.RentalApplications.Add(application);
        await db.SaveChangesAsync();
        return application;
    }

    [Theory]
    [InlineData(ApplicationStatus.Draft)]
    [InlineData(ApplicationStatus.Returned)]
    public async Task SaveApplicantInformationAsync_WhenApplicationIsEditable_Succeeds(ApplicationStatus editableStatus)
    {
        await using var db = InMemoryDbContextFactory.Create();
        var application = await SeedApplicationAsync(db, editableStatus);
        var sut = new ApplicationService(db, userManager: null!, workflow: null!);

        await sut.SaveApplicantInformationAsync(application.Id, "maria",
            new ApplicantInformationInput("Maria Alvarez", "555-0100", "maria@example.com", "1 Main St", Version: 0));

        Assert.Equal("Maria Alvarez", db.ApplicantInformations.Single().FullLegalName);
    }

    [Theory]
    [InlineData(ApplicationStatus.Submitted)]
    [InlineData(ApplicationStatus.Approved)]
    [InlineData(ApplicationStatus.Denied)]
    [InlineData(ApplicationStatus.Withdrawn)]
    public async Task SaveApplicantInformationAsync_WhenApplicationIsNotEditable_ThrowsAndPersistsNothing(ApplicationStatus nonEditableStatus)
    {
        await using var db = InMemoryDbContextFactory.Create();
        var application = await SeedApplicationAsync(db, nonEditableStatus);
        var sut = new ApplicationService(db, userManager: null!, workflow: null!);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.SaveApplicantInformationAsync(application.Id, "maria",
                new ApplicantInformationInput("Maria Alvarez", "555-0100", "maria@example.com", "1 Main St", Version: 0)));

        Assert.Empty(db.ApplicantInformations);
    }
}
