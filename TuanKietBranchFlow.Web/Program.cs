using TuanKietBranchFlow.Web.Components;
using TuanKietBranchFlow.Web.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Đăng ký Blazor Interactive Server.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Đăng ký controller và hỗ trợ filter kiểm tra antiforgery.
builder.Services.AddControllersWithViews();

// Giới hạn request đăng nhập riêng cho từng IP.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode =
        StatusCodes.Status429TooManyRequests;

    // Trả thông báo rõ ràng khi request vượt giới hạn.
    options.OnRejected = async (context, cancellationToken) =>
    {
        HttpResponse response = context.HttpContext.Response;

        response.StatusCode = StatusCodes.Status429TooManyRequests;
        response.ContentType = "text/plain; charset=utf-8";
        response.Headers.CacheControl = "no-store";

        await response.WriteAsync(
            "Bạn đã gửi quá nhiều yêu cầu đăng nhập. "
            + "Vui lòng chờ khoảng 1 phút, quay lại trang đăng nhập và thử lại.",
            cancellationToken);
    };

    options.AddPolicy("WebLogin", httpContext =>
    {
        // Các request cùng IP dùng chung một bộ đếm.
        string ipAddress =
            httpContext.Connection.RemoteIpAddress?.ToString()
            ?? "unknown";

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: ipAddress,
            factory: partitionKey => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            });
    });
});

// Cho phép DI tạo bộ kiểm tra phiên của cookie Web
builder.Services.AddScoped<WebCookieAuthenticationEvents>();

// Đăng ký service phát cookie cho phiên đã lưu trong RAM
builder.Services.AddScoped<WebCookieSignInService>();

// Đăng ký cookie xác thực cho phiên đăng nhập phía Web
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "BranchFlow.WebAuth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;

        // Local cho phép HTTP; production chỉ gửi cookie qua HTTPS
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;

        options.LoginPath = "/login";

        // Kiểm tra phiên RAM khi cookie được xác thực
        options.EventsType = typeof(WebCookieAuthenticationEvents);

        // Không tự kéo dài thời hạn phiên khi người dùng tiếp tục thao tác
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = false;
    });

// Đăng ký dịch vụ phân quyền cho Blazor
builder.Services.AddAuthorization();

// Cho phép các component nhận AuthenticationState dùng chung
builder.Services.AddCascadingAuthenticationState();

// Dùng một kho RAM chung, dữ liệu tách riêng theo mã phiên Web
builder.Services.AddSingleton<IWebAuthSessionStore, WebAuthSessionStore>();

// Tự chạy dịch vụ dọn phiên khi Web khởi động
builder.Services.AddHostedService<WebAuthSessionCleanupService>();

// Blazor nhận trạng thái từ cookie và kiểm tra phiên trong RAM.
builder.Services.AddScoped<
    AuthenticationStateProvider,
    WebAuthenticationStateProvider>();
        
// Đọc địa chỉ API từ file appsettings
string? apiBaseUrl = builder.Configuration["ApiSettings:BaseUrl"];

// Dừng úng dụng sớm nếu chưa cấu hình địa chỉ API
if (string.IsNullOrWhiteSpace(apiBaseUrl))
{
    throw new InvalidOperationException("Chưa cấu hình ApiSettings:BaseUrl.");
}

// Đăng ký HttpClient dùng để gọi BranchFlow API
builder.Services.AddHttpClient(
    "BranchFlowApi", client =>
    {
        client.BaseAddress = new Uri(apiBaseUrl);
    });
// Lấy access token và xử lý refresh cho phiên Web
builder.Services.AddScoped<IWebTokenService, WebTokenService>();

// Service gửi request có Bearer token trong Blazor circuit
builder.Services.AddScoped<AuthorizedApiService>();

// Đăng ký service lấy hồ sơ người dùng hiện tại
builder.Services.AddScoped<UserApiService>();

// Đăng ký service gọi API menu của nhân viên
builder.Services.AddScoped<OrderMenuApiService>();

// Đăng ký service gửi yêu cầu tạo đơn hàng
builder.Services.AddScoped<OrderApiService>();

// Đăng ký service gọi các API chi nhánh
builder.Services.AddScoped<BranchApiService>();

// Đăng ký service gọi các API nhân viên
builder.Services.AddScoped<EmployeeApiService>();

// Đăng ký service gọi API danh mục menu
builder.Services.AddScoped<CategoryApiService>();

// Đăng ký service gọi API size menu
builder.Services.AddScoped<SizeApiService>();

// Đăng ký service gọi API danh sách món
builder.Services.AddScoped<ProductApiService>();

// Đăng ký service gọi API nhóm topping và topping
builder.Services.AddScoped<ToppingGroupApiService>();

// Gọi API quản lý topping bằng token của người dùng hiện tại
builder.Services.AddScoped<ToppingApiService>();

// Caller API cho nhóm ghi chú và lựa chọn con
builder.Services.AddScoped<NoteGroupApiService>();
builder.Services.AddScoped<NoteOptionApiService>();

// Đăng ký service gọi các API xác thực
builder.Services.AddScoped<AuthApiService>();

// Điều phối đăng nhập API và tạo phiên cookie phía Web
builder.Services.AddScoped<WebLoginService>();

// Điều phối thu hồi phiên API, dọn RAM và xóa cookie Web.
builder.Services.AddScoped<WebLogoutService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

app.UseHttpsRedirection();

// Xác định endpoint để đọc policy được gắn trên endpoint đó.
app.UseRouting();

// Đọc cookie hợp lệ để xác định người dùng của request Web
app.UseAuthentication();

// Kiểm tra người dùng có quyền truy cập endpoint hay không
app.UseAuthorization();

// Kiểm tra giới hạn request theo policy của endpoint.
app.UseRateLimiter();

// Bảo vệ các request cần kiểm tra chống giả mạo
app.UseAntiforgery();

app.MapStaticAssets();

// Ánh xạ các route khai báo bằng attribute trong controller Web.
app.MapControllers();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
