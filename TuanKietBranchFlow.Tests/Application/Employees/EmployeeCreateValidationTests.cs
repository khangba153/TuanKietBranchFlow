using System.ComponentModel.DataAnnotations;
using System.Globalization;
using TuanKietBranchFlow.Application.DTOs.Employees;

namespace TuanKietBranchFlow.Tests.Application.Employees;

public class EmployeeCreateValidationTests
{
    [Theory]
    [InlineData("vi-VN")]
    [InlineData("en-US")]
    public void CreateAndUpdate_UseSameSalaryLimitAcrossCultures(string culture)
    {
        CultureInfo original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo(culture);
            EmployeeCreateDTO request = ValidRequest();
            request.BaseSalary = 9999999999999999.99m;
            Assert.Empty(Validate(request));
            request.BaseSalary = 10000000000000000m;
            Assert.Contains(Validate(request), error => error.MemberNames.Contains("BaseSalary"));
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [Fact]
    public void Create_FutureBirthDate_IsRejectedWithFieldError()
    {
        EmployeeCreateDTO request = ValidRequest();
        request.DateOfBirth = DateOnly.FromDateTime(DateTime.Today).AddDays(1);
        Assert.Contains(Validate(request), error => error.MemberNames.Contains("DateOfBirth"));
    }

    [Fact]
    public void Create_DefaultHireDate_IsRejected()
    {
        EmployeeCreateDTO request = ValidRequest();
        request.HireDate = default;
        Assert.Contains(Validate(request), error => error.MemberNames.Contains("HireDate"));
    }

    private static EmployeeCreateDTO ValidRequest()
    {
        return new EmployeeCreateDTO

        {

            Username = "test",
            Password = "TestOnly123!",
            EmployeeCode = "TEST",
            FullName = "Nhân viên thử",
            BranchId = 1,
            HireDate = DateOnly.FromDateTime(DateTime.Today).AddDays(-1)

        };
    }

    private static List<ValidationResult> Validate(EmployeeCreateDTO request)
    {
        List<ValidationResult> errors = new List<ValidationResult>();
        Validator.TryValidateObject(request, new ValidationContext(request), errors, true);
        return errors;
    }
}
