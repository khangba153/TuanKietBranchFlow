using Microsoft.EntityFrameworkCore;
using TuanKietBranchFlow.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

// Lấy connection string từ cấu hình và dừng ứng dụng nếu chưa cấu hình.
string connectionString = 
    builder.Configuration.GetConnectionString("BranchFlowDatabase")
    ?? throw new InvalidOperationException(
        "Chưa cấu hình connection string BranchFlowDatabase.");

// Đăng ký DbContext để các repository có thể truy cập SQL Server.
builder.Services.AddDbContext<BranchFlowDbContext>(options =>
{
    options.UseSqlServer(connectionString);
});

// Đăng ký controller, model binding và chuyển đổi dữ liệu JSON
builder.Services.AddControllers();

// Đăng ký OpenAPI
builder.Services.AddOpenApi();

var app = builder.Build();


if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Ánh xạ các route được khai báo bằng attribute trong controller.
app.MapControllers();

app.Run();
