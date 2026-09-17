using Microsoft.AspNetCore.Authorization;
using PropertyManagement.Domain.Entities;
using PropertyManagement.Domain.Services;

namespace PropertyManagement.Web.Authorization;

public class ApplicationAccessRequirement : IAuthorizationRequirement;

/// <summary>Applicant-side resource check for a RentalApplication. Managers are always
/// permitted (application review is global, not owner-scoped, per spec) so this only ever
/// blocks an applicant from reaching an application they are not on.</summary>
public class ApplicationAccessHandler : AuthorizationHandler<ApplicationAccessRequirement, RentalApplication>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, ApplicationAccessRequirement requirement, RentalApplication resource)
    {
        if (context.User.IsInRole(Roles.PropertyManager))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        var userId = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (userId is not null && ApplicationAccessPolicy.CanApplicantAccess(resource, userId))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
