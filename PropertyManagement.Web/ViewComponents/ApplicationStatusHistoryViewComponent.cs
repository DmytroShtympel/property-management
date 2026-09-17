using Microsoft.AspNetCore.Mvc;
using PropertyManagement.Web.Models.Applications;

namespace PropertyManagement.Web.ViewComponents;

public class ApplicationStatusHistoryViewComponent : ViewComponent
{
    public IViewComponentResult Invoke(List<StatusHistoryItemViewModel> items) => View(items);
}
