namespace TuanKietBranchFlow.Application.DTOs.Auth;

public class LoginResponseDTO
{
    // AccessToken được frontend lưu lại để gọi các API cần đăng nhập
    public string AccessToken { get; set; } = string.Empty;
}