using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PropertyManagement.Domain.Entities;
using PropertyManagement.Domain.Enums;
using PropertyManagement.Domain.Exceptions;
using PropertyManagement.Domain.Services.Validation;
using PropertyManagement.Infrastructure.Services;
using PropertyManagement.Web.Models.Applications;

namespace PropertyManagement.Web.Controllers;

[Authorize]
public class ApplicationsController(
    IApplicationService applications,
    IApplicationReviewService review,
    IPropertyService properties) : Controller
{
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    private bool IsManager => User.IsInRole(Roles.PropertyManager);

    public async Task<IActionResult> Index(ApplicationStatus? status, int? propertyId)
    {
        var items = IsManager
            ? (await review.GetAllAsync(status, propertyId)).Select(ToListItem).ToList()
            : (await applications.GetMyApplicationsAsync(UserId, status, propertyId)).Select(ToListItem).ToList();

        ViewBag.Properties = await properties.GetAllActiveAsync();

        return View(new ApplicationListFilterViewModel
        {
            Status = status,
            PropertyId = propertyId,
            IsManager = IsManager,
            Applications = items
        });
    }

    [HttpGet]
    public async Task<IActionResult> Wizard(int id)
    {
        var application = await applications.GetForWizardAsync(id, UserId);
        if (application is null)
        {
            return Forbid();
        }

        if (application.Status is not (ApplicationStatus.Draft or ApplicationStatus.Returned))
        {
            return RedirectToAction(nameof(Details), new { id });
        }

        return View(BuildWizardViewModel(application));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Wizard(int id, ApplicationWizardPostViewModel model)
    {
        var application = await applications.GetForWizardAsync(id, UserId);
        if (application is null)
        {
            return Forbid();
        }

        if (application.Status is not (ApplicationStatus.Draft or ApplicationStatus.Returned))
        {
            return Forbid();
        }

        // FR-7: Back never saves. Continue/Submit validate the current section first — an invalid
        // section is re-rendered in place with errors and the posted (unsaved) values, leaving
        // persisted data untouched and never advancing.
        if (application.CurrentStep == ApplicationStep.ApplicantInformation && model.Action != "back")
        {
            var candidate = new ApplicantInformation
            {
                FullLegalName = model.FullLegalName ?? "",
                PhoneNumber = model.PhoneNumber ?? "",
                Email = model.Email ?? "",
                CurrentAddress = model.CurrentAddress ?? ""
            };
            var errors = ApplicantInformationValidator.Validate(candidate);
            if (errors.Count > 0)
            {
                var invalidVm = BuildWizardViewModel(application);
                invalidVm.ApplicantInformation = new ApplicantInformationStepViewModel
                {
                    FullLegalName = candidate.FullLegalName,
                    PhoneNumber = candidate.PhoneNumber,
                    Email = candidate.Email,
                    CurrentAddress = candidate.CurrentAddress,
                    Version = model.Version
                };
                invalidVm.CurrentSectionErrors = errors;
                Response.StatusCode = 422;
                return View("Wizard", invalidVm);
            }

            try
            {
                await applications.SaveApplicantInformationAsync(id, UserId, new ApplicantInformationInput(
                    candidate.FullLegalName, candidate.PhoneNumber, candidate.Email, candidate.CurrentAddress, model.Version));
            }
            catch (DbUpdateConcurrencyException)
            {
                TempData["StaleSectionError"] = "Someone else changed the Applicant Information section while you were editing it. Please reload and try again.";
                return RedirectToAction(nameof(Wizard), new { id });
            }
        }

        switch (model.Action)
        {
            case "back":
                if (application.CurrentStep > ApplicationStep.ApplicantInformation)
                {
                    await applications.SetStepAsync(id, UserId, application.CurrentStep - 1);
                }
                break;

            case "submit":
                var fresh = await applications.GetForWizardAsync(id, UserId);
                var blocking = ApplicationSectionValidator.GetBlockingErrors(fresh!);
                if (blocking.Count == 0)
                {
                    try
                    {
                        await applications.SubmitAsync(id, UserId);
                        return RedirectToAction(nameof(Details), new { id });
                    }
                    catch (Exception ex) when (ex is InvalidOperationException or UnitNotAvailableException)
                    {
                        TempData["StaleSectionError"] = ex.Message;
                    }
                }
                break;

            default: // "continue"
                if (application.CurrentStep == ApplicationStep.ResidenceHistory)
                {
                    var sectionErrors = ResidenceHistoryValidator.ValidateSection(application.ResidenceHistoryEntries.ToList());
                    if (sectionErrors.Count > 0)
                    {
                        var invalidVm = BuildWizardViewModel(application);
                        Response.StatusCode = 422;
                        return View("Wizard", invalidVm);
                    }
                }

                if (application.CurrentStep < ApplicationStep.Summary)
                {
                    await applications.SetStepAsync(id, UserId, application.CurrentStep + 1);
                }
                break;
        }

        return RedirectToAction(nameof(Wizard), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> ResidenceHistoryForm(int id, int? entryId)
    {
        var application = await applications.GetForWizardAsync(id, UserId);
        if (application is null)
        {
            return Forbid();
        }

        var model = new ResidenceHistoryFormViewModel { ApplicationId = id };
        if (entryId is int eid)
        {
            var entry = application.ResidenceHistoryEntries.FirstOrDefault(e => e.Id == eid);
            if (entry is null)
            {
                return NotFound();
            }

            model.EntryId = entry.Id;
            model.AddressLine1 = entry.AddressLine1;
            model.City = entry.City;
            model.State = entry.State;
            model.ZipCode = entry.ZipCode;
            model.LandlordName = entry.LandlordName;
            model.LandlordPhone = entry.LandlordPhone;
            model.MoveInDate = entry.MoveInDate;
            model.MoveOutDate = entry.MoveOutDate;
            model.Version = entry.Version;
        }

        return PartialView("_ResidenceHistoryForm", model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ResidenceHistorySave(int id, ResidenceHistoryFormViewModel model)
    {
        if (await applications.GetForWizardAsync(id, UserId) is null)
        {
            return Forbid();
        }

        var input = new ResidenceHistoryInput(
            model.AddressLine1, model.City, model.State, model.ZipCode,
            model.LandlordName, model.LandlordPhone,
            model.MoveInDate ?? default, model.MoveOutDate, model.Version);

        var entryForValidation = new ResidenceHistoryEntry
        {
            AddressLine1 = input.AddressLine1,
            City = input.City,
            State = input.State,
            ZipCode = input.ZipCode,
            LandlordName = input.LandlordName,
            LandlordPhone = input.LandlordPhone,
            MoveInDate = input.MoveInDate,
            MoveOutDate = input.MoveOutDate
        };
        var errors = ResidenceHistoryValidator.ValidateEntry(entryForValidation);
        if (errors.Count > 0)
        {
            model.Errors = errors.ToList();
            Response.StatusCode = 422;
            return PartialView("_ResidenceHistoryForm", model);
        }

        try
        {
            await applications.SaveResidenceEntryAsync(id, UserId, model.EntryId, input);
        }
        catch (DbUpdateConcurrencyException)
        {
            model.Errors = [new FieldError(ResidenceHistoryValidator.SectionName, "Version", "Someone else changed this entry while you were editing it. Please reload and try again.")];
            Response.StatusCode = 422;
            return PartialView("_ResidenceHistoryForm", model);
        }
        catch (InvalidOperationException ex)
        {
            model.Errors = [new FieldError(ResidenceHistoryValidator.SectionName, "Version", ex.Message)];
            Response.StatusCode = 422;
            return PartialView("_ResidenceHistoryForm", model);
        }

        var application = await applications.GetForWizardAsync(id, UserId);
        return PartialView("_ResidenceHistoryList", BuildWizardViewModel(application!));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ResidenceHistoryDelete(int id, int entryId)
    {
        if (await applications.GetForWizardAsync(id, UserId) is null)
        {
            return Forbid();
        }

        try
        {
            await applications.DeleteResidenceEntryAsync(id, UserId, entryId);
        }
        catch (InvalidOperationException ex)
        {
            TempData["StaleSectionError"] = ex.Message;
        }

        var application = await applications.GetForWizardAsync(id, UserId);
        return PartialView("_ResidenceHistoryList", BuildWizardViewModel(application!));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddCoApplicant(int id, string email)
    {
        if (await applications.GetForWizardAsync(id, UserId) is null)
        {
            return Forbid();
        }

        try
        {
            await applications.AddCoApplicantAsync(id, UserId, email);
        }
        catch (InvalidOperationException ex)
        {
            TempData["StaleSectionError"] = ex.Message;
        }

        return RedirectToAction(nameof(Wizard), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Withdraw(int id)
    {
        if (await applications.GetForWizardAsync(id, UserId) is null)
        {
            return Forbid();
        }

        await applications.WithdrawAsync(id, UserId);
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        RentalApplication? application = IsManager
            ? await review.GetDetailsAsync(id)
            : await applications.GetDetailsAsync(id, UserId);

        if (application is null)
        {
            return Forbid();
        }

        return View(BuildDetailsViewModel(application, IsManager));
    }

    private ApplicationWizardViewModel BuildWizardViewModel(RentalApplication application)
    {
        var currentErrors = application.CurrentStep switch
        {
            ApplicationStep.ApplicantInformation => ApplicantInformationValidator.Validate(application.ApplicantInformation),
            ApplicationStep.ResidenceHistory => ResidenceHistoryValidator.ValidateSection(application.ResidenceHistoryEntries.ToList()),
            _ => []
        };

        var blocking = application.CurrentStep == ApplicationStep.Summary
            ? ApplicationSectionValidator.GetBlockingErrors(application)
            : [];

        var latestReturn = application.StatusHistory
            .Where(h => h.ToStatus == ApplicationStatus.Returned)
            .OrderByDescending(h => h.ChangedAtUtc)
            .FirstOrDefault();

        return new ApplicationWizardViewModel
        {
            Id = application.Id,
            Status = application.Status,
            CurrentStep = application.CurrentStep,
            IsReadOnly = application.Status is not (ApplicationStatus.Draft or ApplicationStatus.Returned),
            UnitLabel = $"{application.Unit!.Property!.Name} — Unit {application.Unit.UnitNumber}",
            ApplicantNames = application.Applicants.Select(a => a.User?.FullName ?? a.UserId).ToList(),
            ApplicantInformation = new ApplicantInformationStepViewModel
            {
                FullLegalName = application.ApplicantInformation?.FullLegalName ?? "",
                PhoneNumber = application.ApplicantInformation?.PhoneNumber ?? "",
                Email = application.ApplicantInformation?.Email ?? "",
                CurrentAddress = application.ApplicantInformation?.CurrentAddress ?? "",
                Version = application.ApplicantInformation?.Version ?? 0
            },
            ResidenceHistoryEntries = application.ResidenceHistoryEntries.Select(ToResidenceViewModel).ToList(),
            CurrentSectionErrors = currentErrors,
            BlockingErrors = blocking,
            LatestReturnComment = latestReturn?.Comment,
            StaleSectionMessage = TempData["StaleSectionError"] as string
        };
    }

    private ApplicationDetailsViewModel BuildDetailsViewModel(RentalApplication application, bool isManagerView) => new()
    {
        Id = application.Id,
        Status = application.Status,
        UnitLabel = $"{application.Unit!.Property!.Name} — Unit {application.Unit.UnitNumber}",
        ApplicantNames = application.Applicants.Select(a => a.User?.FullName ?? a.UserId).ToList(),
        ApplicantInformation = new ApplicantInformationStepViewModel
        {
            FullLegalName = application.ApplicantInformation?.FullLegalName ?? "",
            PhoneNumber = application.ApplicantInformation?.PhoneNumber ?? "",
            Email = application.ApplicantInformation?.Email ?? "",
            CurrentAddress = application.ApplicantInformation?.CurrentAddress ?? ""
        },
        ResidenceHistoryEntries = application.ResidenceHistoryEntries.Select(ToResidenceViewModel).ToList(),
        StatusHistory = application.StatusHistory.OrderBy(h => h.ChangedAtUtc).Select(h => new StatusHistoryItemViewModel
        {
            FromStatus = h.FromStatus,
            ToStatus = h.ToStatus,
            ChangedByName = h.ChangedByUser?.FullName,
            ChangedAtUtc = h.ChangedAtUtc,
            Comment = h.Comment
        }).ToList(),
        Lease = application.Lease is null ? null : new LeaseSummaryViewModel
        {
            StartDate = application.Lease.StartDate,
            EndDate = application.Lease.EndDate,
            MonthlyRent = application.Lease.MonthlyRent
        },
        CanWithdraw = !isManagerView && application.Status is ApplicationStatus.Draft or ApplicationStatus.Submitted or ApplicationStatus.UnderReview or ApplicationStatus.Returned,
        CanEdit = !isManagerView && application.Status is ApplicationStatus.Draft or ApplicationStatus.Returned,
        IsManagerView = isManagerView,
        ClaimedByName = application.ClaimedByUser?.FullName,
        ClaimedByCurrentUser = application.ClaimedByUserId == UserId,
        Notes = isManagerView
            ? application.Notes.OrderByDescending(n => n.CreatedAtUtc).Select(n => new ApplicationNoteViewModel
            {
                AuthorName = n.Author?.FullName ?? n.AuthorUserId,
                CreatedAtUtc = n.CreatedAtUtc,
                Body = n.Body
            }).ToList()
            : []
    };

    private static ResidenceHistoryEntryViewModel ToResidenceViewModel(ResidenceHistoryEntry entry) => new()
    {
        Id = entry.Id,
        AddressLine1 = entry.AddressLine1,
        City = entry.City,
        State = entry.State,
        ZipCode = entry.ZipCode,
        LandlordName = entry.LandlordName,
        LandlordPhone = entry.LandlordPhone,
        MoveInDate = entry.MoveInDate,
        MoveOutDate = entry.MoveOutDate,
        Version = entry.Version
    };

    private static ApplicationListItemViewModel ToListItem(RentalApplication application) => new()
    {
        Id = application.Id,
        PropertyName = application.Unit!.Property!.Name,
        UnitNumber = application.Unit.UnitNumber,
        Status = application.Status,
        CreatedAtUtc = application.CreatedAtUtc,
        ApplicantNames = application.Applicants.Select(a => a.User?.FullName ?? a.UserId).ToList()
    };
}
