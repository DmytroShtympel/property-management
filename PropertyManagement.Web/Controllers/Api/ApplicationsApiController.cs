using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PropertyManagement.Domain.Entities;
using PropertyManagement.Domain.Enums;
using PropertyManagement.Infrastructure.Services;
using PropertyManagement.Web.Models.Api;

namespace PropertyManagement.Web.Controllers.Api;

/// <summary>Backs the reusable applications grid view component: paging and sorting are done
/// in the database (see IApplicationService/IApplicationReviewService), scoped the same way as
/// the MVC Applications/Index screen (applicants see their own, managers see all).</summary>
[ApiController]
[Route("api/applications")]
[Authorize]
public class ApplicationsApiController(IApplicationService applications, IApplicationReviewService review) : ControllerBase
{
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    private bool IsManager => User.IsInRole(Roles.PropertyManager);

    /// <summary>Returns a page of applications and the filtered total row count.</summary>
    /// <param name="status">Optional status filter.</param>
    /// <param name="propertyId">Optional property filter.</param>
    /// <param name="page">1-based page number.</param>
    /// <param name="pageSize">Rows per page (max 100).</param>
    /// <param name="sortBy">"createdAt" (default) or "status".</param>
    /// <param name="sortDir">"asc" or "desc" (default).</param>
    [HttpGet]
    [ProducesResponseType<PagedApiResult<ApplicationApiDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedApiResult<ApplicationApiDto>>> Get(
        [FromQuery] ApplicationStatus? status,
        [FromQuery] int? propertyId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? sortBy = "createdAt",
        [FromQuery] string? sortDir = "desc")
    {
        var descending = !string.Equals(sortDir, "asc", StringComparison.OrdinalIgnoreCase);

        var result = IsManager
            ? await review.GetAllPagedAsync(status, propertyId, page, pageSize, sortBy, descending)
            : await applications.GetMyApplicationsPagedAsync(UserId, status, propertyId, page, pageSize, sortBy, descending);

        return Ok(new PagedApiResult<ApplicationApiDto>(
            result.Items.Select(ToDto).ToList(), result.TotalCount, result.Page, result.PageSize));
    }

    private static ApplicationApiDto ToDto(RentalApplication a) => new(
        a.Id,
        a.Unit!.Property!.Name,
        a.Unit.UnitNumber,
        a.Status,
        a.CreatedAtUtc,
        string.Join(", ", a.Applicants.Select(x => x.User?.FullName ?? x.UserId)));
}
