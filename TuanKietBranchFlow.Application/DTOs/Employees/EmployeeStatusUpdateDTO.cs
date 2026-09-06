using System.ComponentModel.DataAnnotations;

namespace TuanKietBranchFlow.Application.DTOs.Employees;

public class EmployeeStatusUpdateDTO
{
    // Trạng thái mới: true là hoạt động, false là nghỉ việc.
    [Required(ErrorMessage = "Trạng thái nhân viên không được để trống.")]
    public bool? IsActive { get; set; }

    // Ngày trạng thái mới bắt đầu có hiệu lực
    public DateOnly EffectiveDate { get; set; }
}