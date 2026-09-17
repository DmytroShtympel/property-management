using PropertyManagement.Domain.Entities;
using PropertyManagement.Domain.Services.Validation;

namespace PropertyManagement.Tests.Domain;

public class SectionValidatorTests
{
    [Fact]
    public void ApplicantInformationValidator_WithNull_ReturnsNotStartedError()
    {
        var errors = ApplicantInformationValidator.Validate(null);

        Assert.Single(errors);
    }

    [Fact]
    public void ApplicantInformationValidator_WithAllFieldsMissing_ReturnsOneErrorPerField()
    {
        var errors = ApplicantInformationValidator.Validate(new ApplicantInformation());

        Assert.Equal(4, errors.Count);
    }

    [Fact]
    public void ApplicantInformationValidator_WithInvalidEmail_ReturnsEmailError()
    {
        var info = new ApplicantInformation
        {
            FullLegalName = "Jordan Lee",
            PhoneNumber = "555-123-4567",
            Email = "not-an-email",
            CurrentAddress = "1 Main St"
        };

        var errors = ApplicantInformationValidator.Validate(info);

        Assert.Single(errors);
        Assert.Equal(nameof(ApplicantInformation.Email), errors[0].Field);
    }

    [Fact]
    public void ApplicantInformationValidator_WithAllFieldsValid_ReturnsNoErrors()
    {
        var info = new ApplicantInformation
        {
            FullLegalName = "Jordan Lee",
            PhoneNumber = "555-123-4567",
            Email = "jordan@example.com",
            CurrentAddress = "1 Main St"
        };

        Assert.Empty(ApplicantInformationValidator.Validate(info));
    }

    [Fact]
    public void ResidenceHistoryValidator_ValidateSection_WithNoEntries_ReturnsAtLeastOneRequiredError()
    {
        var errors = ResidenceHistoryValidator.ValidateSection([]);

        Assert.Single(errors);
    }

    [Fact]
    public void ResidenceHistoryValidator_ValidateEntry_WithMoveOutBeforeMoveIn_ReturnsError()
    {
        var entry = new ResidenceHistoryEntry
        {
            AddressLine1 = "1 Main St",
            City = "Springfield",
            State = "IL",
            ZipCode = "62701",
            LandlordName = "Pat Owner",
            LandlordPhone = "555-000-0000",
            MoveInDate = new DateOnly(2024, 1, 1),
            MoveOutDate = new DateOnly(2023, 1, 1)
        };

        var errors = ResidenceHistoryValidator.ValidateEntry(entry);

        Assert.Contains(errors, e => e.Field == nameof(ResidenceHistoryEntry.MoveOutDate));
    }

    [Fact]
    public void ResidenceHistoryValidator_ValidateEntry_WithoutMoveInDate_ReturnsError()
    {
        var entry = new ResidenceHistoryEntry
        {
            AddressLine1 = "1 Main St",
            City = "Springfield",
            State = "IL",
            ZipCode = "62701",
            LandlordName = "Pat Owner",
            LandlordPhone = "555-000-0000"
        };

        var errors = ResidenceHistoryValidator.ValidateEntry(entry);

        Assert.Contains(errors, e => e.Field == nameof(ResidenceHistoryEntry.MoveInDate));
    }

    [Theory]
    [InlineData(true)] // move-out on/after move-in
    [InlineData(false)] // move-out blank (current residence)
    public void ResidenceHistoryValidator_ValidateEntry_WithValidDateOrdering_ReturnsNoDateErrors(bool includeMoveOut)
    {
        var entry = new ResidenceHistoryEntry
        {
            AddressLine1 = "1 Main St",
            City = "Springfield",
            State = "IL",
            ZipCode = "62701",
            LandlordName = "Pat Owner",
            LandlordPhone = "555-000-0000",
            MoveInDate = new DateOnly(2023, 1, 1),
            MoveOutDate = includeMoveOut ? new DateOnly(2024, 1, 1) : null
        };

        var errors = ResidenceHistoryValidator.ValidateEntry(entry);

        Assert.DoesNotContain(errors, e => e.Field is nameof(ResidenceHistoryEntry.MoveInDate) or nameof(ResidenceHistoryEntry.MoveOutDate));
    }

    [Fact]
    public void ApplicationSectionValidator_WithIncompleteApplicationAndNoHistory_AggregatesBothSections()
    {
        var application = new RentalApplication { Id = 1 };

        var errors = ApplicationSectionValidator.GetBlockingErrors(application);

        Assert.Contains(errors, e => e.Section == ApplicantInformationValidator.SectionName);
        Assert.Contains(errors, e => e.Section == ResidenceHistoryValidator.SectionName);
    }
}
