namespace PropertyManagement.Domain.Exceptions;

/// <summary>Thrown when a manager attempts to decide (Approve/Deny/Return) an application they
/// have not claimed, or to release/act on a claim held by someone else.</summary>
public class ApplicationNotClaimedException(int applicationId, string message)
    : Exception(message)
{
    public int ApplicationId { get; } = applicationId;
}
