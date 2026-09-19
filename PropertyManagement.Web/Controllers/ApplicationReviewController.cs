using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PropertyManagement.Domain.Entities;
using PropertyManagement.Domain.Exceptions;
using PropertyManagement.Domain.Services.Validation;
using PropertyManagement.Infrastructure.Services;
using PropertyManagement.Web.Models;
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
            TempData[TempDataKeys.ReviewError] = "Another manager just claimed this application.";
        }
        catch (Exception ex) when (ex is InvalidOperationException or InvalidApplicationTransitionException or DbUpdateConcurrencyException)
        {
            TempData[TempDataKeys.ReviewError] = "This application changed while you were working on it. Reload the queue and try again.";
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
            TempData[TempDataKeys.ReviewError] = "You do not currently hold the claim on this application.";
        }
        catch (Exception ex) when (ex is InvalidApplicationTransitionException or DbUpdateConcurrencyException)
        {
            TempData[TempDataKeys.ReviewError] = "This application changed while you were working on it. Reload the queue and try again.";
        }

        return RedirectToAction(nameof(Queue));
    }

    [HttpGet]
    public IActionResult ReviewForm(int id) => PartialView("_ReviewForm", new ReviewDecisionViewModel { Id = id });

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Decide(ReviewDecisionViewModel model)
    {
        if (!Enum.TryParse<ReviewOutcome>(model.Outcome, out var outcome))
        {
            return InvalidDecision(model, "Outcome", "Choose an outcome.");
        }

        if (outcome is ReviewOutcome.Deny or ReviewOutcome.Return && string.IsNullOrWhiteSpace(model.Comment))
        {
            return InvalidDecision(model, "Comment", "A comment is required to Return or Deny an application.");
        }

        try
        {
            await review.DecideAsync(model.Id, UserId, outcome, model.Comment);
        }
        catch (Exception ex) when (ex is ApplicationNotClaimedException or InvalidApplicationTransitionException or UnitNotAvailableException)
        {
            return InvalidDecision(model, string.Empty, ex.Message);
        }
        catch (DbUpdateConcurrencyException)
        {
            return InvalidDecision(model, string.Empty, "This application changed while you were reviewing it. Close this window, reload the page and try again.");
        }

        // Success: the modal closes and the application page container is swapped with its refreshed fragment.
        return RedirectToAction("Details", "Applications", new { id = model.Id, fragment = true });
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

    private IActionResult InvalidDecision(ReviewDecisionViewModel model, string field, string message)
    {
        model.Errors = [new FieldError("Review", field, message)];
        Response.StatusCode = 422;
        return PartialView("_ReviewForm", model);
    }
}
