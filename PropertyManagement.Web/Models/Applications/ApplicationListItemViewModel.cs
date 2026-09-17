using PropertyManagement.Domain.Enums;

namespace PropertyManagement.Web.Models.Applications;

public class ApplicationListItemViewModel
{
    public int Id { get; set; }
    public string PropertyName { get; set; } = string.Empty;
    public string UnitNumber { get; set; } = string.Empty;
    public ApplicationStatus Status { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public List<string> ApplicantNames { get; set; } = [];
}

public class ApplicationListFilterViewModel
{
    public ApplicationStatus? Status { get; set; }
    public int? PropertyId { get; set; }
    public bool IsManager { get; set; }
    public List<ApplicationListItemViewModel> Applications { get; set; } = [];
}
