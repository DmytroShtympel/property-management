using PropertyManagement.Domain.Entities;

namespace PropertyManagement.Domain.Services.Validation;

/// <summary>Aggregates every section's validators into the single blocking list the Summary
/// step displays and Submit is gated on ("The Summary lists everything still blocking
/// submission, and Submit stays blocked while any error remains").</summary>
public static class ApplicationSectionValidator
{
    public static IReadOnlyList<FieldError> GetBlockingErrors(RentalApplication application)
    {
        var errors = new List<FieldError>();
        errors.AddRange(ApplicantInformationValidator.Validate(application.ApplicantInformation));
        errors.AddRange(ResidenceHistoryValidator.ValidateSection(application.ResidenceHistoryEntries.ToList()));
        return errors;
    }
}
