using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using PropertyManagement.Domain.Entities;
using PropertyManagement.Infrastructure.Services;
using PropertyManagement.Web.Models;
using PropertyManagement.Web.Models.Units;

namespace PropertyManagement.Web.Controllers;

[Authorize(Roles = Roles.PropertyManager)]
public class UnitsController(IUnitService units, IPropertyService properties, IUnitTypeService unitTypes) : Controller
{
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    public async Task<IActionResult> Index(int propertyId)
    {
        var property = await properties.GetByIdAsync(propertyId, UserId);
        if (property is null)
        {
            return Forbid();
        }

        ViewBag.Property = property;
        var list = await units.GetForPropertyAsync(propertyId, UserId);
        return View(ToListModel(list));
    }

    [HttpGet]
    public async Task<IActionResult> Create(int propertyId)
    {
        var property = await properties.GetByIdAsync(propertyId, UserId);
        if (property is null)
        {
            return Forbid();
        }

        var model = new UnitFormViewModel { PropertyId = propertyId };
        await PopulateUnitTypesAsync(model, currentUnitTypeId: null);
        return PartialView("_UnitForm", model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(UnitFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateUnitTypesAsync(model, currentUnitTypeId: null);
            Response.StatusCode = 422;
            return PartialView("_UnitForm", model);
        }

        try
        {
            await units.CreateAsync(model.PropertyId, UserId, new UnitInput(model.UnitNumber, model.Bedrooms, model.RentAmount, model.UnitTypeId));
        }
        catch (InactiveUnitTypeException)
        {
            ModelState.AddModelError(nameof(model.UnitTypeId), "That unit type is inactive and cannot be selected.");
            await PopulateUnitTypesAsync(model, currentUnitTypeId: null);
            Response.StatusCode = 422;
            return PartialView("_UnitForm", model);
        }
        catch (DuplicateUnitNumberException)
        {
            ModelState.AddModelError(nameof(model.UnitNumber), "That unit number is already used in this property.");
            await PopulateUnitTypesAsync(model, currentUnitTypeId: null);
            Response.StatusCode = 422;
            return PartialView("_UnitForm", model);
        }

        return await RefreshedListAsync(model.PropertyId);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var unit = await units.GetByIdAsync(id, UserId);
        if (unit is null)
        {
            return Forbid();
        }

        var model = new UnitFormViewModel
        {
            Id = unit.Id,
            PropertyId = unit.PropertyId,
            UnitNumber = unit.UnitNumber,
            Bedrooms = unit.Bedrooms,
            RentAmount = unit.RentAmount,
            UnitTypeId = unit.UnitTypeId
        };
        await PopulateUnitTypesAsync(model, currentUnitTypeId: unit.UnitTypeId);
        return PartialView("_UnitForm", model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, UnitFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateUnitTypesAsync(model, currentUnitTypeId: model.UnitTypeId);
            Response.StatusCode = 422;
            return PartialView("_UnitForm", model);
        }

        try
        {
            var updated = await units.UpdateAsync(id, UserId, new UnitInput(model.UnitNumber, model.Bedrooms, model.RentAmount, model.UnitTypeId));
            if (!updated)
            {
                return Forbid();
            }
        }
        catch (InactiveUnitTypeException)
        {
            ModelState.AddModelError(nameof(model.UnitTypeId), "That unit type is inactive and cannot be selected.");
            await PopulateUnitTypesAsync(model, currentUnitTypeId: model.UnitTypeId);
            Response.StatusCode = 422;
            return PartialView("_UnitForm", model);
        }
        catch (DuplicateUnitNumberException)
        {
            ModelState.AddModelError(nameof(model.UnitNumber), "That unit number is already used in this property.");
            await PopulateUnitTypesAsync(model, currentUnitTypeId: model.UnitTypeId);
            Response.StatusCode = 422;
            return PartialView("_UnitForm", model);
        }

        return await RefreshedListAsync(model.PropertyId);
    }

    [HttpGet]
    public async Task<IActionResult> ConfirmRemove(int id)
    {
        var unit = await units.GetByIdAsync(id, UserId);
        if (unit is null)
        {
            return Forbid();
        }

        return PartialView("_ConfirmRemoveModal", new ConfirmRemoveViewModel
        {
            Title = "Remove unit",
            Message = $"Remove unit {unit.UnitNumber}? Its existing applications and leases are kept.",
            PostUrl = Url.Action(nameof(Deactivate), new { id, propertyId = unit.PropertyId })!
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(int id, int propertyId)
    {
        await units.SetRemovedAsync(id, UserId, removed: true);
        return await RefreshedListAsync(propertyId);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Reactivate(int id, int propertyId)
    {
        await units.SetRemovedAsync(id, UserId, removed: false);
        return RedirectToAction(nameof(Index), new { propertyId });
    }

    private async Task<IActionResult> RefreshedListAsync(int propertyId)
    {
        var list = await units.GetForPropertyAsync(propertyId, UserId);
        return PartialView("_UnitsList", ToListModel(list));
    }

    private async Task PopulateUnitTypesAsync(UnitFormViewModel model, int? currentUnitTypeId)
    {
        var selectable = await unitTypes.GetSelectableAsync(currentUnitTypeId);
        model.UnitTypeOptions = selectable.Select(t => new SelectListItem(t.Name, t.Id.ToString())).ToList();
    }

    private static List<UnitListItemViewModel> ToListModel(List<Domain.Entities.Unit> list) =>
        list.Select(u => new UnitListItemViewModel
        {
            Id = u.Id,
            PropertyId = u.PropertyId,
            UnitNumber = u.UnitNumber,
            Bedrooms = u.Bedrooms,
            RentAmount = u.RentAmount,
            UnitTypeName = u.UnitType!.Name,
            UnitTypeIsActive = u.UnitType.IsActive,
            IsRemoved = u.IsRemoved
        }).ToList();
}
