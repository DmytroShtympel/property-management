namespace PropertyManagement.Domain.Services.Validation;

/// <summary>A single validation failure, tied to the specific field it belongs to (per
/// "define rules once per section and return errors to the field they belong to") and,
/// where relevant, the section that owns it so the Summary can group blockers.</summary>
public record FieldError(string Section, string Field, string Message);
