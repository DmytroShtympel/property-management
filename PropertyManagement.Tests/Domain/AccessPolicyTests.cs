using PropertyManagement.Domain.Entities;
using PropertyManagement.Domain.Services;

namespace PropertyManagement.Tests.Domain;

public class AccessPolicyTests
{
    [Fact]
    public void ApplicationAccessPolicy_ApplicantOnTheApplication_CanAccess()
    {
        var application = new RentalApplication { Id = 1 };
        application.Applicants.Add(new ApplicationApplicant { UserId = "applicant-1", IsPrimary = true });
        application.Applicants.Add(new ApplicationApplicant { UserId = "applicant-2", IsPrimary = false });

        Assert.True(ApplicationAccessPolicy.CanApplicantAccess(application, "applicant-1"));
        Assert.True(ApplicationAccessPolicy.CanApplicantAccess(application, "applicant-2"));
    }

    [Fact]
    public void ApplicationAccessPolicy_ApplicantNotOnTheApplication_CannotAccess()
    {
        var application = new RentalApplication { Id = 1 };
        application.Applicants.Add(new ApplicationApplicant { UserId = "applicant-1", IsPrimary = true });

        Assert.False(ApplicationAccessPolicy.CanApplicantAccess(application, "someone-else"));
    }

    [Fact]
    public void PropertyOwnershipPolicy_MatchingOwner_IsOwner()
    {
        var property = new Property { Id = 1, OwnerUserId = "manager-1" };

        Assert.True(PropertyOwnershipPolicy.IsOwner(property, "manager-1"));
        Assert.False(PropertyOwnershipPolicy.IsOwner(property, "manager-2"));
    }
}
