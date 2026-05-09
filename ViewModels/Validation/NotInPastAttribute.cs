using System.ComponentModel.DataAnnotations;

namespace ProviderAssignmentStarter.ViewModels.Validation;

/// <summary>
/// Validates that a <see cref="DateTime"/> property holds today's date or
/// a date in the future.
/// </summary>
/// <remarks>
/// <para>
/// Compares only the <c>.Date</c> portion of the value, so "today at
/// 10am" doesn't fail validation later in the same day at "today at
/// 11am". Local time is used for the comparison because the form
/// presents dates without a time component or timezone — the user's
/// clock is the relevant reference.
/// </para>
/// <para>
/// <b>Conditional skipping via <see cref="SkipIfIdProperty"/>:</b> when
/// the named property on the same VM is non-zero (i.e., this is an
/// existing record being edited), validation is skipped entirely. This
/// lets the same VM be reused for both Create and Edit forms without
/// blocking admins from saving a record whose stored expiration date is
/// already in the past.
/// </para>
/// <para>
/// Usage:
/// </para>
/// <code>
/// [NotInPast(SkipIfIdProperty = nameof(LicenseId),
///            ErrorMessage = "Expiration date must be today or a future date.")]
/// public DateTime ExpirationDate { get; set; }
/// </code>
/// <para>
/// Covered by <c>NotInPastAttributeTests</c>.
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public sealed class NotInPastAttribute : ValidationAttribute
{
    /// <summary>
    /// Name of an integer property on the same instance whose non-zero
    /// value signals that this is an existing record. When set,
    /// validation is skipped and any date is permitted. Use
    /// <c>nameof(YourIdProperty)</c> for compile-time safety.
    /// </summary>
    public string? SkipIfIdProperty { get; set; }

    /// <inheritdoc />
    protected override ValidationResult? IsValid(object? value, ValidationContext ctx)
    {
        // Null lets [Required] (or its absence) decide; this attribute
        // is purely about the date being in the future.
        if (value is null) return ValidationResult.Success;

        // ----- Skip path: existing record (id property is non-zero) -----
        if (!string.IsNullOrEmpty(SkipIfIdProperty))
        {
            var idProp = ctx.ObjectType.GetProperty(SkipIfIdProperty);
            if (idProp?.GetValue(ctx.ObjectInstance) is int id && id != 0)
            {
                return ValidationResult.Success;
            }
        }

        // ----- Validation path: must be today or later -----
        if (value is DateTime dt && dt.Date < DateTime.Today)
        {
            // Pass MemberNames so the error attaches to the correct
            // form field on the client side. Without this the error
            // appears in the validation summary but not next to the
            // input.
            return new ValidationResult(
                FormatErrorMessage(ctx.DisplayName ?? ctx.MemberName ?? "Value"),
                new[] { ctx.MemberName ?? string.Empty });
        }

        return ValidationResult.Success;
    }

    /// <inheritdoc />
    public override string FormatErrorMessage(string name) =>
        $"{name} must be today or a future date.";
}
