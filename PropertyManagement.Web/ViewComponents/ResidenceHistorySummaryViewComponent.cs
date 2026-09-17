using Microsoft.AspNetCore.Mvc;
using PropertyManagement.Web.Models.Applications;

namespace PropertyManagement.Web.ViewComponents;

public class ResidenceHistorySummaryViewComponent : ViewComponent
{
    public IViewComponentResult Invoke(List<ResidenceHistoryEntryViewModel> entries) => View(entries);
}
