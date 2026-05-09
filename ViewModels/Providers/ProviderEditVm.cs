using System.ComponentModel.DataAnnotations;
using ProviderAssignmentStarter.Domain.Enums;

namespace ProviderAssignmentStarter.ViewModels.Providers;

/// <summary>
/// Form-bound view model for both the Create and Edit Provider pages.
/// </summary>
/// <remarks>
/// <para>
/// Validation lives here via DataAnnotations so:
/// <list type="bullet">
///   <item>The Razor controller stays thin (just <c>ModelState.IsValid</c>).</item>
///   <item>Client-side validation comes for free via the unobtrusive validation script.</item>
///   <item>Server-side validation happens automatically as part of model binding.</item>
/// </list>
/// </para>
/// <para>
/// <see cref="ProviderId"/> is 0 on Create and non-zero on Edit. The
/// controller uses this to differentiate the two operations on the same VM.
/// </para>
/// </remarks>
public class ProviderEditVm
{
    /// <summary>
    /// 0 when creating a new provider; the existing provider's id when
    /// editing. Hidden field on the Edit form; not bound on Create.
    /// </summary>
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
