using System.ComponentModel.DataAnnotations;

namespace TuanKietBranchFlow.Application.DTOs.Auth;

public class RefreshTokenRequestDTO
{
    // Token gốc dùng để yêu cầu cấp cặp token mới
    [Required(ErrorMessage = "Refresh token không được để trống.")]
    [StringLength(
        44,
        MinimumLength = 44,
        ErrorMessage = "Refresh token phải có đúng 44 ký tự.")]
    public string RefreshToken { get; set; } = string.Empty;
}