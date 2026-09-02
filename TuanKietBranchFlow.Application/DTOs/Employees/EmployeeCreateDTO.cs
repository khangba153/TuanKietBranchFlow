using System.ComponentModel.DataAnnotations;

namespace TuanKietBranchFlow.Application.DTOs.Employees;

public class EmployeeCreateDTO
{
    // Thông tin dùng để tạo tài khoản đăng nhập
    [Required(ErrorMessage = "Username không được để trống.")]
    [StringLength(100, ErrorMessage = "Username không được vượt quá 100 ký tự.")]
    public string Username { get; set; } = string.Empty;

    // Backend sẽ hash password trước khi lưu
    [Required(ErrorMessage = "Mật khẩu không được để trống.")]
    [StringLength(100, ErrorMessage = "Mật khẩu không được vượt quá 100 ký tự.")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Họ tên không được để trống.")]
    [StringLength(150, ErrorMessage = "Họ và tên không được vượt quá 150 ký tự.")]
    public string FullName { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "Email không đúng định dạng.")]
    [StringLength(254, ErrorMessage = "Email không được vượt quá 254 ký tự.")]
    public string? Email { get; set; }

    [StringLength(30, ErrorMessage = "Số điện thoại không được vượt quá 30 ký tự.")]
    public string? Phone { get; set; }

    // Thông tin dùng để tạo hồ sơ nhân viên
    [Required(ErrorMessage = "Mã nhân viên không được để trống.")]
    [StringLength(30, ErrorMessage = "Mã nhân viên không được vượt quá 30 ký tự.")]
    public string EmployeeCode { get; set; } = string.Empty;
    public DateOnly? DateOfBirth { get; set; }
    public DateOnly HireDate { get; set; }

    [StringLength(100, ErrorMessage = "Chức vụ không được vượt quá 100 ký tự.")]
    public string? Position { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Lương cơ bản không được nhỏ hơn 0.")]
    public decimal BaseSalary { get; set; }
    
    [StringLength(500, ErrorMessage = "Địa chỉ không được vượt quá 500 ký tự.")]
    public string? Address { get; set; }

    // Chi nhánh mà ADMIN đang chọn để phân công nhân viên
    [Range(1, int.MaxValue, ErrorMessage = "BranchId phải lớn hơn 0.")]
    public int BranchId { get; set; } 
} 