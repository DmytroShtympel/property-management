namespace PropertyManagement.Web.Models.Applications;

public class ApplicantInformationStepViewModel
{
    public string FullLegalName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string CurrentAddress { get; set; } = string.Empty;
    public int Version { get; set; }
}
