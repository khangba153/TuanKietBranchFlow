using System.ComponentModel.DataAnnotations;

namespace TuanKietBranchFlow.Application.DTOs.Employees;

public static class EmployeeProfileValidation
{
    // Tạo và sửa dùng cùng quy tắc ngày, không tự thêm quy định tuổi lao động.
    public static IEnumerable<ValidationResult> ValidateDates(
        DateOnly hireDate,
        DateOnly? dateOfBirth)
    {
        List<ValidationResult> errors = new List<ValidationResult>();
        DateOnly today = DateOnly.FromDateTime(DateTime.Today);
        if (hireDate == default || hireDate > today)
        {
            errors.Add(new ValidationResult("Ngày vào làm không được để trống hoặc lớn hơn ngày hiện tại.", new[]
            {
                "HireDate"
            }));
        }

        if (dateOfBirth.HasValue && dateOfBirth.Value > today)
        {
            errors.Add(new ValidationResult("Ngày sinh không được lớn hơn ngày hiện tại.", new[]
            {
                "DateOfBirth"
            }));
        }

        return errors;
    }
}
