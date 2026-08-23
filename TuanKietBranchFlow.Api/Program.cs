using Microsoft.EntityFrameworkCore;
using TuanKietBranchFlow.Infrastructure.Data;
using TuanKietBranchFlow.Infrastructure.UnitOfWork;
using Microsoft.OpenApi;

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

// Đăng ký UnitOfWork để các service có 1 điểm lưu dữ liệu thống nhất.
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Đăng ký controller, model binding và chuyển đổi dữ liệu JSON
builder.Services.AddControllers();

// Đăng ký bộ sinh tài liệu Swagger
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "BranchFlow API",
        Version = "v1",
        Description = "API quản lý hệ thống bán đồ uống BranchFlow."
    });
});



var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    // Tạo endpoint chứa tài liệu Swagger dạng JSON.
    app.UseSwagger();

    // Hiển thị giao diện Swagger để kiểm thử API.
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint(
            "/swagger/v1/swagger.json",
            "BranchFlow API v1");
    });
}

app.UseHttpsRedirection();

// Ánh xạ các route được khai báo bằng attribute trong controller.
app.MapControllers();

app.Run();
