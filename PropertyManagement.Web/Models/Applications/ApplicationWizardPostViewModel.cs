namespace PropertyManagement.Web.Models.Applications;

/// <summary>The single wizard form posts to a single action; which button was clicked
/// (continue/back/submit) is carried in Action, per "One form posts to one action, and the
/// button clicked determines what happens." Applicant Information fields are only meaningful
/// when the application is currently on that step.</summary>
public class ApplicationWizardPostViewModel
{
    public string Action { get; set; } = "continue";

    public string? FullLegalName { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public string? CurrentAddress { get; set; }
    public int Version { get; set; }
}
