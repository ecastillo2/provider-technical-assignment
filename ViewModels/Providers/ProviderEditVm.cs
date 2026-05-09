using System.ComponentModel.DataAnnotations;
using ProviderAssignmentStarter.Domain.Enums;

namespace ProviderAssignmentStarter.ViewModels.Providers;

/// <summary>
/// Bound to the Create / Edit forms. Validation lives here so the
/// controller stays thin and so client-side validation just works.
/// </summary>
public class ProviderEditVm
{
    public int ProviderId { get; set; }

    [Required(ErrorMessage = "Provider name is required.")]
    [StringLength(200, MinimumLength = 2, ErrorMessage = "Name must be 2-200 characters.")]
    [Display(Name = "Provider Name")]
    public string ProviderName { get; set; } = string.Empty;

    [Required(ErrorMessage = "County is required.")]
    [StringLength(100, MinimumLength = 2)]
    public string County { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Lifecycle Status")]
    public ProviderStatus Status { get; set; } = ProviderStatus.Pending;
}
