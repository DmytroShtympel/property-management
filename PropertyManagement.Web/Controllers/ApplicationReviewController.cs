using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PropertyManagement.Domain.Entities;
using PropertyManagement.Domain.Exceptions;
using PropertyManagement.Infrastructure.Services;
using PropertyManagement.Web.Models.Review;

namespace PropertyManagement.Web.Controllers;

[Authorize(Roles = Roles.PropertyManager)]
public class ApplicationReviewController(IApplicationReviewService review) : Controller
{
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    public async Task<IActionResult> Queue()
    {
        var items = await review.GetQueueAsync();
        return View(items.Select(a => new ReviewQueueItemViewModel
        {
            Id = a.Id,
            PropertyName = a.Unit!.Property!.Name,
            UnitNumber = a.Unit.UnitNumber,
            ApplicantNames = a.Applicants.Select(x => x.User?.FullName ?? x.UserId).ToList(),
            SubmittedAtUtc = a.SubmittedAtUtc,
            ClaimedByName = a.ClaimedByUser?.FullName,
            ClaimedByCurrentUser = a.ClaimedByUserId == UserId
        }).ToList());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Claim(int id)
    {
        try
        {
            await review.ClaimAsync(id, UserId);
        }
        catch (ApplicationAlreadyClaimedException)
        {
            TempData["ReviewError"] = "Another manager just claimed this application.";
        }

        return RedirectToAction(nameof(Queue));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Release(int id)
    {
        try
        {
            await review.ReleaseAsync(id, UserId);
        }
        catch (ApplicationNotClaimedException)
        {
            TempData["ReviewError"] = "You do not currently hold the claim on this application.";
        }

        return RedirectToAction(nameof(Queue));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Decide(ReviewDecisionViewModel model)
    {
        if (!Enum.TryParse<ReviewOutcome>(model.Outcome, out var outcome))
        {
            TempData["ReviewError"] = "Choose an outcome.";
            return RedirectToAction("Details", "Applications", new { id = model.Id });
        }

        if (outcome is ReviewOutcome.Deny or ReviewOutcome.Return && string.IsNullOrWhiteSpace(model.Comment))
        {
            TempData["ReviewError"] = "A comment is required to Return or Deny an application.";
            return RedirectToAction("Details", "Applications", new { id = model.Id });
        }

        try
        {
            await review.DecideAsync(model.Id, UserId, outcome, model.Comment);
        }
        catch (Exception ex) when (ex is ApplicationNotClaimedException or InvalidApplicationTransitionException or UnitNotAvailableException)
        {
            TempData["ReviewError"] = ex.Message;
        }

        return RedirectToAction("Details", "Applications", new { id = model.Id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddNote(int id, string body)
    {
        if (!string.IsNullOrWhiteSpace(body))
        {
            await review.AddNoteAsync(id, UserId, body);
        }

        return RedirectToAction("Details", "Applications", new { id });
    }
}
