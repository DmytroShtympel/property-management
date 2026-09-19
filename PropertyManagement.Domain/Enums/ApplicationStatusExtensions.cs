namespace PropertyManagement.Domain.Enums;

public static class ApplicationStatusExtensions
{
    public static string DisplayName(this ApplicationStatus status) =>
        status == ApplicationStatus.UnderReview ? "Under Review" : status.ToString();
}
