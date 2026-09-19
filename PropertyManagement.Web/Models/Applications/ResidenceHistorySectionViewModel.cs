namespace PropertyManagement.Web.Models.Applications;

public class ResidenceHistorySectionViewModel
{
    public int ApplicationId { get; init; }
    public IReadOnlyList<ResidenceHistoryEntryViewModel> Entries { get; init; } = [];
    public bool IsReadOnly { get; init; }
}
