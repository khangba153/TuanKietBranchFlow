using TuanKietBranchFlow.Web.Components;

var builder = WebApplication.CreateBuilder(args);

// Đăng ký Blazor Interactive Server.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Đọc địa chỉ API từ file appsettings
string? apiBaseUrl = builder.Configuration["ApiSettings:BaseUrl"];

// Dừng úng dụng sớm nếu chưa cấu hình địa chỉ API
if (string.IsNullOrWhiteSpace(apiBaseUrl))
{
    throw new InvalidOperationException("Chưa cấu hình ApiSettings:BaseUrl.");
}

// Đăng ký HttpClient dùng để gọi BranchFlow API
builder.Services.AddHttpClient(
    "BranchFlowApi",client =>
    {
        client.BaseAddress = new Uri(apiBaseUrl);
    });

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
