using Microsoft.AspNetCore.Mvc;
using PropertyManagement.Domain.Enums;

namespace PropertyManagement.Web.ViewComponents;

public class ApplicationStatusBadgeViewComponent : ViewComponent
{
    public IViewComponentResult Invoke(ApplicationStatus status)
    {
        // Palette per DESIGN.md: Submitted=info (waiting), UnderReview=primary (someone's on it
        // now — visually distinct from the passive "waiting" blue), Withdrawn shares Draft's
        // color and is distinguished by strikethrough instead of a seventh badge color.
        var cssClass = status switch
        {
            ApplicationStatus.Draft => "bg-secondary",
            ApplicationStatus.Submitted => "bg-info",
            ApplicationStatus.UnderReview => "bg-primary",
            ApplicationStatus.Returned => "bg-warning text-dark",
            ApplicationStatus.Approved => "bg-success",
            ApplicationStatus.Denied => "bg-danger",
            ApplicationStatus.Withdrawn => "bg-secondary text-decoration-line-through",
            _ => "bg-secondary"
        };

        return View(model: (status.DisplayName(), cssClass));
    }
}
