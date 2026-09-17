namespace PropertyManagement.Domain.Exceptions;

public class ApplicationAlreadyClaimedException(int applicationId, string claimedByUserId)
    : Exception($"Application {applicationId} is already claimed by another manager.")
{
    public int ApplicationId { get; } = applicationId;
    public string ClaimedByUserId { get; } = claimedByUserId;
}
