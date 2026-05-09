using System.ComponentModel.DataAnnotations;
using ProviderAssignmentStarter.Domain.Enums;
using ProviderAssignmentStarter.ViewModels.Validation;

namespace ProviderAssignmentStarter.ViewModels.Licenses;

/// <summary>
/// Form-bound view model for both the Create and Edit License pages.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="LicenseId"/> is 0 on Create and non-zero on Edit. The
/// custom <see cref="NotInPastAttribute"/> on <see cref="ExpirationDate"/>
/// uses that distinction to enforce different rules:
/// </para>
/// <list type="bullet">
///   <item><b>Create:</b> the expiration date must be today or later. We never want a brand-new license that's already expired.</item>
///   <item><b>Edit:</b> any date is allowed. Admins must be able to correct historical records — if a row was seeded with an expiration of last year, the form should still be saveable.</item>
/// </list>
/// </remarks>
public class LicenseEditVm
{
    /// <summary>0 when creating; the existing id when editing.</summary>
    public int LicenseId { get; set; }

    /// <summary>FK to the parent Provider. Always required.</summary>
    [Required]
    public int ProviderId { get; set; }

    /// <summary>
    /// The parent's display name. Display only — not used as a binding
    /// target. Hidden field on the form so it survives a round-trip
    /// when validation fails.
    /// </summary>
    public string ProviderName { get; set; } = string.Empty;

    [Required(ErrorMessage = "License number is required.")]
    [StringLength(50, MinimumLength = 2)]
    [Display(Name = "License Number")]
    public string LicenseNumber { get; set; } = string.Empty;

    [Required]
    [Display(Name = "License Status")]
    public LicenseStatus LicenseStatus { get; set; } = LicenseStatus.Active;

    /// <summary>
    /// Expiration date. Validated by <see cref="NotInPastAttribute"/> on
    /// Create; allowed to be any date on Edit (see remarks at the top of
    /// this class). Default value is one year out for the Create form's
    /// convenience.
    /// </summary>
    [Required]
    [DataType(DataType.Date)]
    [Display(Name = "Expiration Date")]
    [NotInPast(SkipIfIdProperty = nameof(LicenseId),
               ErrorMessage = "Expiration date must be today or a future date.")]
    public DateTime ExpirationDate { get; set; } = DateTime.UtcNow.Date.AddYears(1);
}
