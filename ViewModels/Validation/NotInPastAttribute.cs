using System.ComponentModel.DataAnnotations;

namespace ProviderAssignmentStarter.ViewModels.Validation;

/// <summary>
/// Validates that a <see cref="DateTime"/> property is today's date or
/// later. Compares only the date part so "today at 10am" doesn't fail
/// at "today at 11am".
///
/// Optional <see cref="SkipIfIdProperty"/> lets the same VM be reused for
/// both Create and Edit: when the named id property is non-zero (i.e.,
/// an existing record), validation is skipped so admins can edit a
/// record whose stored expiration is already in the past.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public sealed class NotInPastAttribute : ValidationAttribute
{
    public string? SkipIfIdProperty { get; set; }

    protected override ValidationResult? IsValid(object? value, ValidationContext ctx)
    {
        if (value is null) return ValidationResult.Success; // [Required] handles nulls.

        if (!string.IsNullOrEmpty(SkipIfIdProperty))
        {
            var idProp = ctx.ObjectType.GetProperty(SkipIfIdProperty);
            if (idProp?.GetValue(ctx.ObjectInstance) is int id && id != 0)
            {
                return ValidationResult.Success; // existing record - allow any date
            }
        }

        if (value is DateTime dt && dt.Date < DateTime.Today)
        {
            return new ValidationResult(
                FormatErrorMessage(ctx.DisplayName ?? ctx.MemberName ?? "Value"),
                new[] { ctx.MemberName ?? string.Empty });
        }

        return ValidationResult.Success;
    }

    public override string FormatErrorMessage(string name) =>
        $"{name} must be today or a future date.";
}
