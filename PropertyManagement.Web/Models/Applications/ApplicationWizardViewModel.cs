using PropertyManagement.Domain.Enums;
using PropertyManagement.Domain.Services.Validation;

namespace PropertyManagement.Web.Models.Applications;

public class ApplicationWizardViewModel : ApplicationSectionsViewModel
{
    public ApplicationStatus Status { get; set; }
    public ApplicationStep CurrentStep { get; set; }
    public bool IsReadOnly { get; set; }
    public string UnitLabel { get; set; } = string.Empty;

    /// <summary>Every outstanding error across all sections — only populated on the Summary
    /// step, where Submit is gated on this being empty.</summary>
    public IReadOnlyList<FieldError> BlockingErrors { get; set; } = [];

    public string? LatestReturnComment { get; set; }
    public string? StaleSectionMessage { get; set; }

    /// <summary>A section is editable only while the wizard is showing it and the application is still Draft or Returned.</summary>
    public override bool IsSectionReadOnly(ApplicationStep section) => IsReadOnly || section != CurrentStep;
}
