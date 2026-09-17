using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PropertyManagement.Domain.Entities;
using PropertyManagement.Infrastructure.Services;
using PropertyManagement.Web.Authorization;
using PropertyManagement.Web.Models.Properties;

namespace PropertyManagement.Web.Controllers;

[Authorize(Roles = Roles.PropertyManager)]
public class PropertiesController(IPropertyService properties, IAuthorizationService authorizationService) : Controller
{
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    public async Task<IActionResult> Index(bool showRemoved = false)
    {
        var list = await properties.GetOwnedAsync(UserId, includeRemoved: showRemoved);
        ViewBag.ShowRemoved = showRemoved;
        return View(await ToListPartialModelAsync(list));
    }

    [HttpGet]
    public IActionResult Create() => PartialView("_PropertyForm", new PropertyFormViewModel());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PropertyFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            Response.StatusCode = 422;
            return PartialView("_PropertyForm", model);
        }

        await properties.CreateAsync(UserId, new PropertyInput(model.Name, model.AddressLine1, model.AddressLine2, model.City, model.State, model.ZipCode));
        return await RefreshedListAsync();
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var property = await properties.GetByIdAsync(id, UserId);
        if (property is null || !(await authorizationService.AuthorizeAsync(User, property, new PropertyOwnerRequirement())).Succeeded)
        {
            return Forbid();
        }

        var model = new PropertyFormViewModel
        {
            Id = property.Id,
            Name = property.Name,
            AddressLine1 = property.AddressLine1,
            AddressLine2 = property.AddressLine2,
            City = property.City,
            State = property.State,
            ZipCode = property.ZipCode
        };
        return PartialView("_PropertyForm", model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, PropertyFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            Response.StatusCode = 422;
            return PartialView("_PropertyForm", model);
        }

        var input = new PropertyInput(model.Name, model.AddressLine1, model.AddressLine2, model.City, model.State, model.ZipCode);
        var updated = await properties.UpdateAsync(id, UserId, input);
        if (!updated)
        {
            return Forbid();
        }

        return await RefreshedListAsync();
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(int id)
    {
        await properties.SetRemovedAsync(id, UserId, removed: true);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Reactivate(int id)
    {
        await properties.SetRemovedAsync(id, UserId, removed: false);
        return RedirectToAction(nameof(Index), new { showRemoved = true });
    }

    private async Task<IActionResult> RefreshedListAsync()
    {
        var list = await properties.GetOwnedAsync(UserId, includeRemoved: false);
        return PartialView("_PropertiesList", await ToListPartialModelAsync(list));
    }

    private Task<List<PropertyListItemViewModel>> ToListPartialModelAsync(List<Property> list) =>
        Task.FromResult(list.Select(p => new PropertyListItemViewModel
        {
            Id = p.Id,
            Name = p.Name,
            AddressLine1 = p.AddressLine1,
            City = p.City,
            State = p.State,
            ZipCode = p.ZipCode,
            UnitCount = p.Units.Count,
            IsRemoved = p.IsRemoved
        }).ToList());
}
