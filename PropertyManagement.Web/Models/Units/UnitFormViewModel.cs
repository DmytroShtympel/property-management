using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace PropertyManagement.Web.Models.Units;

public class UnitFormViewModel
{
    public int Id { get; set; }
    public int PropertyId { get; set; }

    [Required, Display(Name = "Unit number")]
    public string UnitNumber { get; set; } = string.Empty;

    [Range(0, 10)]
    public int Bedrooms { get; set; }

    [Range(0.01, 100000), Display(Name = "Monthly rent")]
    public decimal RentAmount { get; set; }

    [Required, Display(Name = "Unit type")]
    public int UnitTypeId { get; set; }

    public List<SelectListItem> UnitTypeOptions { get; set; } = [];
}
