using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PropertyManagement.Domain.Entities;
using PropertyManagement.Infrastructure.Services;
using PropertyManagement.Web.Models.Listings;

namespace PropertyManagement.Web.Controllers;

public class ListingsController(IListingService listings, IApplicationService applications) : Controller
{
    public async Task<IActionResult> Index()
    {
        var units = await listings.GetAvailableUnitsAsync();
        return View(units.Select(ToViewModel).ToList());
    }

    public async Task<IActionResult> Details(int id)
    {
        var unit = await listings.GetAvailableUnitByIdAsync(id);
        if (unit is null)
        {
            return NotFound();
        }

        return View(ToViewModel(unit));
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = Roles.Applicant)]
    public async Task<IActionResult> Apply(int unitId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var application = await applications.StartOrResumeDraftAsync(unitId, userId);
        return RedirectToAction("Wizard", "Applications", new { id = application.Id });
    }

    private static UnitBrowseListItemViewModel ToViewModel(Unit unit) => new()
    {
        Id = unit.Id,
        PropertyName = unit.Property!.Name,
        City = unit.Property.City,
        State = unit.Property.State,
        UnitNumber = unit.UnitNumber,
        Bedrooms = unit.Bedrooms,
        RentAmount = unit.RentAmount,
        UnitTypeName = unit.UnitType!.Name
    };
}
