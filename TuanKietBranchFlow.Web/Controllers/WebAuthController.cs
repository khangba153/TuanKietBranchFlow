using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TuanKietBranchFlow.Application.DTOs.Auth;
using TuanKietBranchFlow.Web.Services;
using Microsoft.AspNetCore.RateLimiting;
using System.Net;

namespace TuanKietBranchFlow.Web.Controllers;

// Nhận form đăng nhập để xử lý cookie trên Web server.
[Route("auth")]
public class WebAuthController : Controller
{
    private readonly WebLoginService _webLoginService;
    private readonly WebLogoutService _webLogoutService;

    public WebAuthController(
        WebLoginService webLoginService,
        WebLogoutService webLogoutService)
    {
        _webLoginService = webLoginService;
        _webLogoutService = webLogoutService;
    }

    /// <summary>
    /// Đăng nhập qua API, lưu phiên Web và phát cookie cho trình duyệt.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("login")]
    [EnableRateLimiting("WebLogin")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(16 * 1024)]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> LoginAsync(
        [FromForm] LoginRequestDTO request)
    {
        // Kiểm tra các điều kiện validation trên DTO.
        if (!ModelState.IsValid)
        {
            return Redirect("/login?error=invalid-input");
        }

        try
        {
            bool isSuccess = await _webLoginService.LoginAsync(
                HttpContext,
                request);

            // API không chấp nhận thông tin đăng nhập.
            if (!isSuccess)
            {
                return Redirect("/login?error=rejected");
            }

            return Redirect("/");
        }
        catch (HttpRequestException exception)
            when (exception.StatusCode == HttpStatusCode.TooManyRequests)
        {
            // Giữ đúng mã 429, không báo nhầm là sai mật khẩu.
            return Problem(
                statusCode: StatusCodes.Status429TooManyRequests,
                title: "Quá nhiều yêu cầu đăng nhập",
                detail: "Hệ thống đang giới hạn yêu cầu đăng nhập. "
                    + "Vui lòng chờ khoảng 1 phút, quay lại trang đăng nhập "
                    + "và thử lại.");
        }
    }
    
    /// <summary>
    /// Thu hồi các phiên API, dọn phiên RAM và xóa cookie đăng nhập Web.
    /// </summary>
    [Authorize]
    [HttpPost("logout")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(16 * 1024)]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> LogoutAsync()
    {
        try
        {
            bool isApiLogoutConfirmed =
                await _webLogoutService.LogoutAsync(HttpContext);

            // Web đã kết thúc phiên hiện tại nhưng chưa xác nhận logout-all.
            if (!isApiLogoutConfirmed)
            {
                return Redirect("/login?error=logout-unconfirmed");
            }

            return Redirect("/login");
        }
        catch (HttpRequestException)
        {
            return Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "Chưa xác nhận được việc đăng xuất",
                detail: "Không thể hoàn tất yêu cầu thu hồi phiên với API. "
                    + "Vui lòng kiểm tra kết nối và thử lại.");
        }
        catch (TaskCanceledException)
        {
            return Problem(
                statusCode: StatusCodes.Status504GatewayTimeout,
                title: "Yêu cầu đăng xuất quá thời gian chờ",
                detail: "Chưa xác nhận được việc thu hồi các phiên. "
                    + "Vui lòng thử lại.");
        }
    }
}