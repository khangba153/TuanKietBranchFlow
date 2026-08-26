namespace TuanKietBranchFlow.Application.DTOs.Users;

public class UserProfileDTO
{
    // Thông tin tài khoản
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;

    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string Role { get; set; } = string.Empty;

    // Thông tin hồ sơ nhân viên
    public string? EmployeeCode { get; set; }
    public DateOnly? HireDate { get; set; }
    public string? Position { get; set; }
    public string? Address { get; set; }
    public string? AvatarUrl { get; set; }

    // Chi nhánh hiện tại của người dùng
    public int? CurrentBranchId { get; set; }
    public string? CurrentBranchName { get; set; }
}