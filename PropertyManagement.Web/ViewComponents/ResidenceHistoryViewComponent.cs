using Microsoft.AspNetCore.Mvc;
using PropertyManagement.Web.Models.Applications;

namespace PropertyManagement.Web.ViewComponents;

public class ResidenceHistoryViewComponent : ViewComponent
{
    public IViewComponentResult Invoke(int applicationId, List<ResidenceHistoryEntryViewModel> entries, bool isReadOnly) =>
        View(new ResidenceHistorySectionViewModel { ApplicationId = applicationId, Entries = entries, IsReadOnly = isReadOnly });
}
