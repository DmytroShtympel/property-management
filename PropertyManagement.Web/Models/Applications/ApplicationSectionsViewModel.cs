using PropertyManagement.Domain.Enums;
using PropertyManagement.Domain.Services.Validation;

namespace PropertyManagement.Web.Models.Applications;

/// <summary>What the section partials and view components render; the wizard and the read-only application page both derive from it.</summary>
public abstract class ApplicationSectionsViewModel
{
    public int Id { get; set; }
    public List<string> ApplicantNames { get; set; } = [];

    public ApplicantInformationStepViewModel ApplicantInformation { get; set; } = new();
    public List<ResidenceHistoryEntryViewModel> ResidenceHistoryEntries { get; set; } = [];

    /// <summary>Field errors for whichever section is currently displayed (inline display).</summary>
    public IReadOnlyList<FieldError> CurrentSectionErrors { get; set; } = [];

    /// <summary>The server-side decision every section partial obeys: true renders that section read-only.</summary>
    public abstract bool IsSectionReadOnly(ApplicationStep section);
}
