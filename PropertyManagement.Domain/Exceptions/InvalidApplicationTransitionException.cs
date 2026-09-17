using PropertyManagement.Domain.Enums;

namespace PropertyManagement.Domain.Exceptions;

public class InvalidApplicationTransitionException(ApplicationStatus from, ApplicationStatus to)
    : Exception($"Cannot transition application from '{from}' to '{to}'.")
{
    public ApplicationStatus From { get; } = from;
    public ApplicationStatus To { get; } = to;
}
