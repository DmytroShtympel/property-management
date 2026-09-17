namespace PropertyManagement.Domain.Common;

public interface ISoftDeletable
{
    bool IsRemoved { get; set; }
}
