namespace TuanKietBranchFlow.Application.DTOs.Auth;

public class CurrentUserDTO
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}