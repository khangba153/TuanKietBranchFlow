using TuanKietBranchFlow.Web.Components;
using TuanKietBranchFlow.Web.Services;
using Microsoft.AspNetCore.Components.Authorization;

var builder = WebApplication.CreateBuilder(args);

// Đăng ký Blazor Interactive Server.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Đăng ký dịch vụ thao tác với LocalStorage của trình duyệt
builder.Services.AddLocalStorageServices();

// Đăng ký dịch vụ phân quyền cho Blazor
builder.Services.AddAuthorization();

// Cho phép các component nhận AuthenticationState dùng chung
builder.Services.AddCascadingAuthenticationState();

// Đăng ký provider quản lý trạng thái đăng nhập của giao diện
builder.Services.AddScoped<CustomAuthenticationStateProvider>();

// Khi compenent yêu cầu AuthenticationStateProvider DI sẽ trả đúng CustomAuthenticationStateProvider
builder.Services.AddScoped<AuthenticationStateProvider>(
    serviceProvider =>
        serviceProvider.GetRequiredService<CustomAuthenticationStateProvider>());
        
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
// Đăng ký handler tự động gắn access token
builder.Services.AddScoped<ApiAuthorizationHandler>();
// Đăng ký service gọi các API xác thực
builder.Services.AddScoped<AuthApiService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

app.UseHttpsRedirection();
app.UseAntiforgery();

app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
