using PropertyManagement.Domain.Enums;

namespace PropertyManagement.Tests.Domain;

public class ApplicationStatusExtensionsTests
{
    [Fact]
    public void DisplayName_SplitsUnderReviewIntoTwoWords()
    {
        Assert.Equal("Under Review", ApplicationStatus.UnderReview.DisplayName());
    }

    [Theory]
    [InlineData(ApplicationStatus.Draft, "Draft")]
    [InlineData(ApplicationStatus.Submitted, "Submitted")]
    [InlineData(ApplicationStatus.Returned, "Returned")]
    [InlineData(ApplicationStatus.Approved, "Approved")]
    [InlineData(ApplicationStatus.Denied, "Denied")]
    [InlineData(ApplicationStatus.Withdrawn, "Withdrawn")]
    public void DisplayName_UsesTheEnumNameForSingleWordStatuses(ApplicationStatus status, string expected)
    {
        Assert.Equal(expected, status.DisplayName());
    }
}
