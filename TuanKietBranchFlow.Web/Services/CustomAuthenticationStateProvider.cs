using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using TuanKietBranchFlow.Application.DTOs.Auth;
using Microsoft.JSInterop;

namespace TuanKietBranchFlow.Web.Services;

public class CustomAuthenticationStateProvider : AuthenticationStateProvider
{
    // Lưu thông tin người dùng hiện tại của giao diện
    // ClaimsIdentity rỗng có nghĩa là người dùng chưa đăng nhập
    private ClaimsPrincipal _currentUser =
        new ClaimsPrincipal(new ClaimsIdentity());
    private readonly ILocalStorageService _localStorage;
    private readonly AuthApiService _authApiService;

    // Nhận dịch vụ đọc LocalStorage và gọi API xác thực
    public CustomAuthenticationStateProvider(
        ILocalStorageService localStorage,
        AuthApiService authApiService)
    {
        _localStorage = localStorage;
        _authApiService = authApiService;
    }

    // Trả trạng thái đăng nhập hiện tại cho Blazor
    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        AuthenticationState authenticationState =
            new AuthenticationState(_currentUser);
        
        return Task.FromResult(authenticationState);
    }

    // Đánh dấu người dùng đã đăng nhập sau khi API xác nhận token
    public void MarkUserAsAuthenticated(CurrentUserDTO currentUser)
    {
        // Chuyển thông tin từ DTO thành các claim của người dùng
        List<Claim> claims = new List<Claim>
        {
            new Claim(
                ClaimTypes.NameIdentifier,
                currentUser.UserId.ToString()),

            new Claim(
                ClaimTypes.Name,
                currentUser.Username),
            
            new Claim(
                ClaimTypes.Role,
                currentUser.Role)
        };
        
        // AuthenticationType phải có giá trị để Identity.IsAuthenticated nhận kết quả true
        ClaimsIdentity identity =
            new ClaimsIdentity(claims, "JwtAuthentication");
        
        _currentUser = new ClaimsPrincipal(identity);

        // Thông báo cho Blazor biết trạng thái đăng nhập đã thay đổi
        NotifyAuthenticationStateChanged(Task.FromResult(
            new AuthenticationState(_currentUser)));
    }

    // Khôi phục người dùng từ access token sau khi tải lại trang
    public async Task RestoreAuthenticationStateAsync()
    {
        // Đọc access token đang lưu trong trình duyệt
        string? accessToken =
            await _localStorage.GetItemAsync<string>("accessToken");

        // Không có token thì giữ trạng thái chưa đăng nhập
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return;
        }

        // Gửi token đến API để xác nhận và lấy thông tin người dùng
        CurrentUserDTO? currentUser =
            await _authApiService.GetCurrentUserAsync(accessToken);

        // Token hết hạn hoặc không hợp lệ thì xóa khỏi trình duyệt
        if (currentUser == null)
        {
            await _localStorage.RemoveItemAsync("accessToken");
    
            return;
        }

        // Token hợp lệ thì tạo lại trạng thái đăng nhập cho Blazor
        MarkUserAsAuthenticated(currentUser);
    }

    // Xóa token và đưa giao diện về trạng thái chưa đăng nhập
    public async Task LogoutAsync()
    {
        // Xóa access token khỏi trình duyệt
        await _localStorage.RemoveItemAsync("accessToken");

        // Tạo lại người dùng anonymous
        _currentUser = new ClaimsPrincipal(new ClaimsIdentity());

        // Thông báo cho AuthorizeView cập nhật giao diện
        NotifyAuthenticationStateChanged(Task.FromResult(
            new AuthenticationState(_currentUser)));
    }
}