namespace PropertyManagement.Domain.Entities;

public static class Roles
{
    public const string PropertyManager = "PropertyManager";
    public const string Applicant = "Applicant";

    public static readonly string[] All = [PropertyManager, Applicant];
}
