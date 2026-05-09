using System.ComponentModel.DataAnnotations;
using FluentAssertions;
using ProviderAssignmentStarter.Domain.Enums;
using ProviderAssignmentStarter.ViewModels.Licenses;
using Xunit;

namespace ProviderAssignmentStarter.Tests.Validation;

public class NotInPastAttributeTests
{
    private static IList<ValidationResult> Validate(LicenseEditVm vm)
    {
        var ctx = new ValidationContext(vm);
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(vm, ctx, results, validateAllProperties: true);
        return results;
    }

    [Fact]
    public void Create_form_rejects_an_expiration_date_in_the_past()
    {
        var vm = new LicenseEditVm
        {
            // LicenseId = 0  --> create mode
            ProviderId     = 1,
            LicenseNumber  = "X-1",
            LicenseStatus  = LicenseStatus.Active,
            ExpirationDate = DateTime.Today.AddDays(-1)
        };

        var results = Validate(vm);

        results.Should().Contain(r =>
            r.MemberNames.Contains(nameof(LicenseEditVm.ExpirationDate)));
    }

    [Fact]
    public void Create_form_accepts_an_expiration_date_of_today_or_later()
    {
        var vm = new LicenseEditVm
        {
            ProviderId     = 1,
            LicenseNumber  = "X-1",
            LicenseStatus  = LicenseStatus.Active,
            ExpirationDate = DateTime.Today
        };

        Validate(vm).Should().NotContain(r =>
            r.MemberNames.Contains(nameof(LicenseEditVm.ExpirationDate)));
    }

    [Fact]
    public void Edit_form_allows_a_past_expiration_date()
    {
        // Existing record (LicenseId != 0) - admins can save without
        // touching the date even though it is already in the past.
        var vm = new LicenseEditVm
        {
            LicenseId      = 42,
            ProviderId     = 1,
            LicenseNumber  = "X-1",
            LicenseStatus  = LicenseStatus.Expired,
            ExpirationDate = DateTime.Today.AddYears(-1)
        };

        Validate(vm).Should().NotContain(r =>
            r.MemberNames.Contains(nameof(LicenseEditVm.ExpirationDate)));
    }
}
