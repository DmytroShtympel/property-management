using Microsoft.AspNetCore.Mvc;

namespace PropertyManagement.Web.ViewComponents;

/// <summary>Reusable grid shell (bonus 1): renders sortable/pageable table markup that is
/// populated client-side from GET /api/applications, so the paging/sorting logic lives once
/// in the database query (see IApplicationService/IApplicationReviewService) and once on the
/// client (wwwroot/js/applicationsGrid.js), instead of being re-implemented per screen.</summary>
public class ApplicationsGridViewComponent : ViewComponent
{
    public IViewComponentResult Invoke() => View();
}
