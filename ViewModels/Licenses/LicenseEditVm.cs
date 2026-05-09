using System.ComponentModel.DataAnnotations;
using ProviderAssignmentStarter.Domain.Enums;
using ProviderAssignmentStarter.ViewModels.Validation;

namespace ProviderAssignmentStarter.ViewModels.Licenses;

public class LicenseEditVm
{
    public int LicenseId { get; set; }

    [Required]
    public int ProviderId { get; set; }

    /// <summary>Display only - read from the parent Provider on the form.</summary>
    public string ProviderName { get; set; } = string.Empty;

    [Required(ErrorMessage = "License number is required.")]
    [StringLength(50, MinimumLength = 2)]
    [Display(Name = "License Number")]
    public string LicenseNumber { get; set; } = string.Empty;

    [Required]
    [Display(Name = "License Status")]
    public LicenseStatus LicenseStatus { get; set; } = LicenseStatus.Active;

    [Required]
    [DataType(DataType.Date)]
    [Display(Name = "Expiration Date")]
    [NotInPast(SkipIfIdProperty = nameof(LicenseId),
               ErrorMessage = "Expiration date must be today or a future date.")]
    public DateTime ExpirationDate { get; set; } = DateTime.UtcNow.Date.AddYears(1);
}
