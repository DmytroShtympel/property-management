using Microsoft.AspNetCore.Authorization;
using PropertyManagement.Domain.Entities;
using PropertyManagement.Domain.Services;

namespace PropertyManagement.Web.Authorization;

public class PropertyOwnerRequirement : IAuthorizationRequirement;

public class PropertyOwnerHandler : AuthorizationHandler<PropertyOwnerRequirement, Property>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PropertyOwnerRequirement requirement, Property resource)
    {
        var userId = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (userId is not null && PropertyOwnershipPolicy.IsOwner(resource, userId))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
