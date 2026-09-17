using System.ComponentModel.DataAnnotations;

namespace PropertyManagement.Web.Models.UnitTypes;

public class UnitTypeFormViewModel
{
    public int Id { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty;
}
