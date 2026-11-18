using System.ComponentModel.DataAnnotations;
using TuanKietBranchFlow.Application.DTOs.Employees;

namespace TuanKietBranchFlow.Tests.Application.Employees;

public class EmployeeUpdateValidationTests
{
    [Fact]
    public void Update_FutureDates_ReturnsFieldSpecificErrors()
    {
        EmployeeUpdateDTO request = CreateRequest();
        request.HireDate = DateOnly.FromDateTime(DateTime.Today).AddDays(1);
        request.DateOfBirth = request.HireDate;
        List<ValidationResult> errors = new List<ValidationResult>();
        bool valid = Validator.TryValidateObject(request, new ValidationContext(request), errors, true);
        Assert.False(valid);
        Assert.Contains(errors, error => error.MemberNames.Contains(nameof(request.HireDate)));
        Assert.Contains(errors, error => error.MemberNames.Contains(nameof(request.DateOfBirth)));
    }

    [Theory]
    [InlineData("-1")]
    [InlineData("10000000000000000")]
    public void Update_SalaryOutsideSqlRange_IsInvalid(string salary)
    {
        EmployeeUpdateDTO request = CreateRequest();
        request.BaseSalary = decimal.Parse(salary, System.Globalization.CultureInfo.InvariantCulture);
        List<ValidationResult> errors = new List<ValidationResult>();
        Assert.False(Validator.TryValidateObject(request, new ValidationContext(request), errors, true));
        Assert.Contains(errors, error => error.MemberNames.Contains(nameof(request.BaseSalary)));
    }

    [Fact]
    public void Update_DefaultHireDate_IsInvalid()
    {
        EmployeeUpdateDTO request = CreateRequest();
        request.HireDate = default;
        List<ValidationResult> errors = new List<ValidationResult>();
        Assert.False(Validator.TryValidateObject(request, new ValidationContext(request), errors, true));
    }

    [Fact]
    public void Update_ValidProfileWithOptionalFieldsEmpty_IsValid()
    {
        EmployeeUpdateDTO request = CreateRequest();
        List<ValidationResult> errors = new List<ValidationResult>();
        Assert.True(Validator.TryValidateObject(request, new ValidationContext(request), errors, true));
    }

    [Theory]
    [InlineData("vi-VN")]
    [InlineData("en-US")]
    public void Update_SalaryLimit_IsConsistentAcrossCultures(string cultureName)
    {
        System.Globalization.CultureInfo originalCulture = System.Globalization.CultureInfo.CurrentCulture;
        try
        {
            System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo(cultureName);
            EmployeeUpdateDTO request = CreateRequest();
            request.BaseSalary = 9999999999999999.99m;
            List<ValidationResult> errors = new List<ValidationResult>();
            Assert.True(Validator.TryValidateObject(request, new ValidationContext(request), errors, true));
        }
        finally
        {
            System.Globalization.CultureInfo.CurrentCulture = originalCulture;
        }
    }

    private static EmployeeUpdateDTO CreateRequest()
    {
        return new EmployeeUpdateDTO

        {

            FullName = "Nguyễn Thị Thủy",
            EmployeeCode = "NV-TEST",
            HireDate = DateOnly.FromDateTime(DateTime.Today).AddDays(-1),
            BaseSalary = 7000000

        };
    }
}
