namespace PropertyManagement.Web.Models.Units;

public class UnitListItemViewModel
{
    public int Id { get; set; }
    public int PropertyId { get; set; }
    public string UnitNumber { get; set; } = string.Empty;
    public int Bedrooms { get; set; }
    public decimal RentAmount { get; set; }
    public string UnitTypeName { get; set; } = string.Empty;
    public bool UnitTypeIsActive { get; set; }
    public bool IsRemoved { get; set; }
}
