using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PropertyManagement.Domain.Entities;
using PropertyManagement.Infrastructure.Services;
using PropertyManagement.Web.Models.UnitTypes;

namespace PropertyManagement.Web.Controllers;

[Authorize(Roles = Roles.PropertyManager)]
public class UnitTypesController(IUnitTypeService unitTypes) : Controller
{
    public async Task<IActionResult> Index() => View(await unitTypes.GetAllAsync());

    [HttpGet]
    public IActionResult Create() => PartialView("_UnitTypeForm", new UnitTypeFormViewModel());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(UnitTypeFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            Response.StatusCode = 422;
            return PartialView("_UnitTypeForm", model);
        }

        await unitTypes.CreateAsync(model.Name);
        return await RefreshedListAsync();
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var type = await unitTypes.GetByIdAsync(id);
        if (type is null)
        {
            return NotFound();
        }

        return PartialView("_UnitTypeForm", new UnitTypeFormViewModel { Id = type.Id, Name = type.Name });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, UnitTypeFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            Response.StatusCode = 422;
            return PartialView("_UnitTypeForm", model);
        }

        await unitTypes.UpdateAsync(id, model.Name);
        return await RefreshedListAsync();
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(int id, bool isActive)
    {
        await unitTypes.SetActiveAsync(id, isActive);
        return RedirectToAction(nameof(Index));
    }

    private async Task<IActionResult> RefreshedListAsync() =>
        PartialView("_UnitTypesList", await unitTypes.GetAllAsync());
}
