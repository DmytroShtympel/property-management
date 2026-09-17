using PropertyManagement.Domain.Entities;

namespace PropertyManagement.Domain.Services.Validation;

public static class ResidenceHistoryValidator
{
    public const string SectionName = "Residence History";

    /// <summary>Validates a single entry, e.g. when saving it from the modal.</summary>
    public static IReadOnlyList<FieldError> ValidateEntry(ResidenceHistoryEntry entry)
    {
        var errors = new List<FieldError>();

        if (string.IsNullOrWhiteSpace(entry.AddressLine1))
        {
            errors.Add(new FieldError(SectionName, nameof(entry.AddressLine1), "Address is required."));
        }

        if (string.IsNullOrWhiteSpace(entry.City))
        {
            errors.Add(new FieldError(SectionName, nameof(entry.City), "City is required."));
        }

        if (string.IsNullOrWhiteSpace(entry.State))
        {
            errors.Add(new FieldError(SectionName, nameof(entry.State), "State is required."));
        }

        if (string.IsNullOrWhiteSpace(entry.ZipCode))
        {
            errors.Add(new FieldError(SectionName, nameof(entry.ZipCode), "Zip code is required."));
        }

        if (string.IsNullOrWhiteSpace(entry.LandlordName))
        {
            errors.Add(new FieldError(SectionName, nameof(entry.LandlordName), "Landlord name is required."));
        }

        if (string.IsNullOrWhiteSpace(entry.LandlordPhone))
        {
            errors.Add(new FieldError(SectionName, nameof(entry.LandlordPhone), "Landlord phone is required."));
        }

        if (entry.MoveInDate == default)
        {
            errors.Add(new FieldError(SectionName, nameof(entry.MoveInDate), "Move-in date is required."));
        }

        if (entry.MoveOutDate is { } moveOut && moveOut < entry.MoveInDate)
        {
            errors.Add(new FieldError(SectionName, nameof(entry.MoveOutDate), "Move-out date cannot be before the move-in date."));
        }

        return errors;
    }

    /// <summary>Section-level rule: at least one residence history entry is required. Each
    /// existing entry is assumed already-valid at save time (validated via ValidateEntry when
    /// it was added/edited through the modal), so this only re-checks entry-level validity
    /// defensively for the Summary/Submit gate.</summary>
    public static IReadOnlyList<FieldError> ValidateSection(IReadOnlyCollection<ResidenceHistoryEntry> entries)
    {
        if (entries.Count == 0)
        {
            return [new FieldError(SectionName, "Entries", "At least one residence history entry is required.")];
        }

        return entries.SelectMany(ValidateEntry).ToList();
    }
}
