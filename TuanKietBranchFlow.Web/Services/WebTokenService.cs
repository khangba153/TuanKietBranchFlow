using System.Net;
using System.Net.Http.Json;
using TuanKietBranchFlow.Application.DTOs.Auth;
using TuanKietBranchFlow.Web.Models;

namespace TuanKietBranchFlow.Web.Services;

// Xử lý token của các phiên đăng nhập phía Web server
public class WebTokenService : IWebTokenService
{
    private readonly IWebAuthSessionStore _sessionStore;
    private readonly IHttpClientFactory _httpClientFactory;

    // Nhận kho phiên và factory tạo client gọi API
    public WebTokenService(
        IWebAuthSessionStore sessionStore,
        IHttpClientFactory httpClientFactory)
    {
        _sessionStore = sessionStore;
        _httpClientFactory = httpClientFactory;
    }
    
    // Lấy access token và đồng bộ refresh của riêng phiên Web
    public async Task<string?> GetAccessTokenAsync(Guid webSessionId)
    {
        WebAuthSession? session = _sessionStore.GetById(webSessionId);

        if (session == null)
        {
            return null;
        }

        // Chờ quyền xử lý token của phiên này
        await session.TokenLock.WaitAsync();

        try
        {
            // Phiên có thể đã bị xóa trong lúc request chờ khóa
            if (_sessionStore.GetById(webSessionId) != session)
            {
                return null;
            }

            // Giữ khoảng đệm 30 giây trước khi token hết hạn
            if (!string.IsNullOrWhiteSpace(session.AccessToken) &&
                session.AccessTokenExpiresAt > DateTime.UtcNow.AddSeconds(30))
            {
                return session.AccessToken;
            }

            LoginResponseDTO? tokenResponse;

            try
            {
                tokenResponse = await RefreshTokensAsync(session.RefreshToken);

                // Chỉ nhận cặp token đầy đủ, không gia hạn phiên ban đầu
                if (tokenResponse != null &&
                    (string.IsNullOrWhiteSpace(tokenResponse.AccessToken) ||
                    string.IsNullOrWhiteSpace(tokenResponse.RefreshToken) ||
                    tokenResponse.RefreshToken.Length != 44 ||
                    tokenResponse.RefreshToken == session.RefreshToken ||
                    tokenResponse.AccessTokenExpiresAt <= DateTime.UtcNow ||
                    tokenResponse.RefreshTokenExpiresAt !=
                        session.RefreshTokenExpiresAt ||
                    tokenResponse.AccessTokenExpiresAt >
                        tokenResponse.RefreshTokenExpiresAt))
                {
                    throw new InvalidOperationException(
                        "API refresh trả dữ liệu token không hợp lệ.");
                }
            }
            catch
            {
                // Không gửi lại token cũ khi kết quả rotation không chắc chắn
                _sessionStore.Remove(webSessionId);
                throw;
            }

            // API xác nhận không được tiếp tục dùng phiên
            if (tokenResponse == null)
            {
                _sessionStore.Remove(webSessionId);
                return null;
            }

            // Không khôi phục phiên đã bị xóa trong lúc chờ API
            if (_sessionStore.GetById(webSessionId) != session)
            {
                return null;
            }

            // Cập nhật cả cặp token khi vẫn đang giữ khóa
            session.AccessToken = tokenResponse.AccessToken;
            session.RefreshToken = tokenResponse.RefreshToken;
            session.AccessTokenExpiresAt = tokenResponse.AccessTokenExpiresAt;
            session.RefreshTokenExpiresAt = tokenResponse.RefreshTokenExpiresAt;

            return session.AccessToken;
        }
        finally
        {
            // Luôn nhả khóa sau khi đã lấy được khóa
            session.TokenLock.Release();
        }
    }

    // Gửi token gốc để xin cặp token mới, không cần Bearer
    private async Task<LoginResponseDTO?> RefreshTokensAsync(
        string refreshToken)
    {
        HttpClient httpClient =
            _httpClientFactory.CreateClient("BranchFlowApi");

        RefreshTokenRequestDTO request = new RefreshTokenRequestDTO
        {
            RefreshToken = refreshToken
        };

        using HttpResponseMessage response =
            await httpClient.PostAsJsonAsync(
                "api/auth/refresh",
                request);

        // API từ chối token hoặc phiên
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            return null;
        }

        // Không biến lỗi hệ thống thành kết quả hết phiên
        response.EnsureSuccessStatusCode();

        LoginResponseDTO? tokenResponse =
            await response.Content.ReadFromJsonAsync<LoginResponseDTO>();

        if (tokenResponse == null)
        {
            throw new InvalidOperationException(
                "API refresh trả thành công nhưng không có dữ liệu token.");
        }

        return tokenResponse;
    }
}