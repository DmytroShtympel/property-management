using PropertyManagement.Domain.Enums;
using PropertyManagement.Domain.Services.Validation;

namespace PropertyManagement.Web.Models.Applications;

public class ApplicationWizardViewModel
{
    public int Id { get; set; }
    public ApplicationStatus Status { get; set; }
    public ApplicationStep CurrentStep { get; set; }
    public bool IsReadOnly { get; set; }
    public string UnitLabel { get; set; } = string.Empty;
    public List<string> ApplicantNames { get; set; } = [];

    public ApplicantInformationStepViewModel ApplicantInformation { get; set; } = new();
    public List<ResidenceHistoryEntryViewModel> ResidenceHistoryEntries { get; set; } = [];

    /// <summary>Field errors for whichever section is currently displayed (inline display).</summary>
    public IReadOnlyList<FieldError> CurrentSectionErrors { get; set; } = [];

    /// <summary>Every outstanding error across all sections — only populated on the Summary
    /// step, where Submit is gated on this being empty.</summary>
    public IReadOnlyList<FieldError> BlockingErrors { get; set; } = [];

    public string? LatestReturnComment { get; set; }
    public string? StaleSectionMessage { get; set; }
}
