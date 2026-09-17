namespace PropertyManagement.Web.Models.Listings;

public class UnitBrowseListItemViewModel
{
    public int Id { get; set; }
    public string PropertyName { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string UnitNumber { get; set; } = string.Empty;
    public int Bedrooms { get; set; }
    public decimal RentAmount { get; set; }
    public string UnitTypeName { get; set; } = string.Empty;
}
