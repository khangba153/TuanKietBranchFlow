using System.ComponentModel.DataAnnotations;

namespace TuanKietBranchFlow.Application.DTOs.Auth;

public class LoginRequestDTO
{
    // Username là thông tin dùng để tìm tài khoản trong db
    [Required(ErrorMessage = "Username không được để trống.")]
    [StringLength(100, ErrorMessage = "Username không được vượt quá 100 ký tự.")]
    public string Username { get; set; } = string.Empty;

    // Password chỉ dùng để kiểm tra, không trả cho frontend
    [Required(ErrorMessage = "Mật khẩu không được để trống.")]
    [StringLength(100, ErrorMessage = "Mật khẩu không được vượt quá 100 ký tự.")]
    public string Password { get; set; } = string.Empty;
}