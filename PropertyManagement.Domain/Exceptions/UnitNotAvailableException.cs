namespace PropertyManagement.Domain.Exceptions;

public class UnitNotAvailableException(int unitId)
    : Exception("This unit already has an active lease and can no longer be submitted or approved.")
{
    public int UnitId { get; } = unitId;
}
