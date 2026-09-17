using System.ComponentModel.DataAnnotations;

namespace PropertyManagement.Web.Models.Properties;

public class PropertyFormViewModel
{
    public int Id { get; set; }

    [Required, Display(Name = "Property name")]
    public string Name { get; set; } = string.Empty;

    [Required, Display(Name = "Address line 1")]
    public string AddressLine1 { get; set; } = string.Empty;

    [Display(Name = "Address line 2")]
    public string? AddressLine2 { get; set; }

    [Required]
    public string City { get; set; } = string.Empty;

    [Required]
    public string State { get; set; } = string.Empty;

    [Required, Display(Name = "Zip code")]
    public string ZipCode { get; set; } = string.Empty;
}
