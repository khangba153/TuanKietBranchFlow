using System.ComponentModel.DataAnnotations;

namespace TuanKietBranchFlow.Application.DTOs.Employees;

public class EmployeeChangeBranchDTO
{
    // Chi nhánh nhân viên sẽ được chuyển đến
    [Range(1, int.MaxValue, ErrorMessage = "NewBranchId phải lớn hơn 0.")]
    public int NewBranchId { get; set; }

    // Ngày nhân viên bắt đầu làm tại chi nhánh mới
    public DateOnly ActiveFrom { get; set; }
}