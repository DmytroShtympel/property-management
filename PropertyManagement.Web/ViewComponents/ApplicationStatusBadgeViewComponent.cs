using Microsoft.AspNetCore.Mvc;
using PropertyManagement.Domain.Enums;

namespace PropertyManagement.Web.ViewComponents;

public class ApplicationStatusBadgeViewComponent : ViewComponent
{
    public IViewComponentResult Invoke(ApplicationStatus status)
    {
        var cssClass = status switch
        {
            ApplicationStatus.Draft => "bg-secondary",
            ApplicationStatus.Submitted => "bg-primary",
            ApplicationStatus.UnderReview => "bg-info text-dark",
            ApplicationStatus.Returned => "bg-warning text-dark",
            ApplicationStatus.Approved => "bg-success",
            ApplicationStatus.Denied => "bg-danger",
            ApplicationStatus.Withdrawn => "bg-dark",
            _ => "bg-secondary"
        };

        return View(model: (status, cssClass));
    }
}
