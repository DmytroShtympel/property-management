using PropertyManagement.Domain.Entities;

namespace PropertyManagement.Domain.Services;

/// <summary>Applicant-side ownership check for a RentalApplication. Manager access to
/// applications is global (not scoped) per spec, so there is no manager-side policy here.</summary>
public static class ApplicationAccessPolicy
{
    public static bool CanApplicantAccess(RentalApplication application, string applicantUserId) =>
        application.Applicants.Any(a => a.UserId == applicantUserId);
}
