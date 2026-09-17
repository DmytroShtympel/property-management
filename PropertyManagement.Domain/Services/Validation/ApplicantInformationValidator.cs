using System.Text.RegularExpressions;
using PropertyManagement.Domain.Entities;

namespace PropertyManagement.Domain.Services.Validation;

public static partial class ApplicantInformationValidator
{
    public const string SectionName = "Applicant Information";

    public static IReadOnlyList<FieldError> Validate(ApplicantInformation? info)
    {
        if (info is null)
        {
            return [new FieldError(SectionName, nameof(ApplicantInformation.FullLegalName), "Applicant Information has not been started.")];
        }

        var errors = new List<FieldError>();

        if (string.IsNullOrWhiteSpace(info.FullLegalName))
        {
            errors.Add(new FieldError(SectionName, nameof(info.FullLegalName), "Full legal name is required."));
        }

        if (string.IsNullOrWhiteSpace(info.PhoneNumber))
        {
            errors.Add(new FieldError(SectionName, nameof(info.PhoneNumber), "Phone number is required."));
        }

        if (string.IsNullOrWhiteSpace(info.Email))
        {
            errors.Add(new FieldError(SectionName, nameof(info.Email), "Email is required."));
        }
        else if (!EmailRegex().IsMatch(info.Email))
        {
            errors.Add(new FieldError(SectionName, nameof(info.Email), "Email is not a valid address."));
        }

        if (string.IsNullOrWhiteSpace(info.CurrentAddress))
        {
            errors.Add(new FieldError(SectionName, nameof(info.CurrentAddress), "Current address is required."));
        }

        return errors;
    }

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailRegex();
}
