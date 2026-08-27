using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using TuanKietBranchFlow.Infrastructure.Data;
using TuanKietBranchFlow.Infrastructure.UnitOfWork;
using Microsoft.OpenApi;
using TuanKietBranchFlow.Application.Services;
using TuanKietBranchFlow.Infrastructure.Repositories;
using Microsoft.AspNetCore.Identity;
using TuanKietBranchFlow.Infrastructure.Models;

var builder = WebApplication.CreateBuilder(args);

// Đọc cấu hình dùng để kiểm tra JWT
string jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Chưa cấu hình Jwt:Key trong User Secrets.");

string jwtIssuer = builder.Configuration["Jwt:Issuer"]
    ?? throw new InvalidOperationException("Chưa cấu hình Jwt:Issuer.");

string jwtAudience = builder.Configuration["Jwt:Audience"]
    ?? throw new InvalidOperationException("Chưa cấu hình Jwt:Audience.");
// Đọc số phút tồn tại của access token
int jwtExpireMinutes = builder.Configuration.GetValue<int?>("Jwt:ExpireMinutes") ?? 60;

if (jwtExpireMinutes <= 0)
{
    throw new InvalidOperationException("Jwt:ExpireMinutes phải lớn hơn 0.");
}

// Lấy connection string từ cấu hình và dừng ứng dụng nếu chưa cấu hình
string connectionString = 
    builder.Configuration.GetConnectionString("BranchFlowDatabase")
    ?? throw new InvalidOperationException(
        "Chưa cấu hình connection string BranchFlowDatabase.");

// Đăng ký DbContext để các repository có thể truy cập SQL Server
builder.Services.AddDbContext<BranchFlowDbContext>(options =>
{
    options.UseSqlServer(connectionString);
});

// Đăng ký repository phục vụ truy vấn tài khoản
builder.Services.AddScoped<IUserRepository, UserRepository>();

// Đăng ký repository để truy vấn chi nhánh
builder.Services.AddScoped<IBranchRepository, BranchRepository>();

// Đăng ký repository để truy vấn danh sách Role
builder.Services.AddScoped<IRoleRepository, RoleRepository>();

// Đăng ký công cụ tạo và kiểm tra PasswordHash
builder.Services.AddScoped<IPasswordHasher<AppUser>, PasswordHasher<AppUser>>();

// Đăng ký service xử lý nghiệp vụ đăng nhập
builder.Services.AddScoped<IAuthService, AuthService>();

// Đăng ký service xử lý hồ sơ người dùng
builder.Services.AddScoped<IUserService, UserService>();

// Đăng ký service xử lý nghiệp vụ phạm vi chi nhánh
builder.Services.AddScoped<IBranchService, BranchService>();

// Đăng ký service xử lý danh sách Role
builder.Services.AddScoped<IRoleService, RoleService>();

// Đăng ký UnitOfWork để các service có 1 điểm lưu dữ liệu thống nhất
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Đăng ký service tạo JWT từ cấu hình của API
builder.Services.AddScoped<JwtTokenService>(servicePorvider =>
{
    return new JwtTokenService(jwtKey, jwtIssuer, jwtAudience, jwtExpireMinutes);
});
// Đăng ký controller, model binding và chuyển đổi dữ liệu JSON
builder.Services.AddControllers();

// Đăng ký cơ chế xác thực bằng JWT Bearer
builder.Services
.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    // Quy định các điều kiên của 1 JWT hợp lệ
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),

        ValidateIssuer = true,
        ValidIssuer = jwtIssuer,

        ValidateAudience = true,
        ValidAudience = jwtAudience,

        ValidateLifetime = true,

        // Token hết hạn là từ chối
        ClockSkew = TimeSpan.Zero,

        // Quy định claim đại diện tên và role
        NameClaimType = ClaimTypes.Name,
        RoleClaimType = ClaimTypes.Role
    };
});

// Đăng ký dịch vụ kiểm tra quyền truy cập
builder.Services.AddAuthorization();

// Đăng ký bộ sinh tài liệu Swagger
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "BranchFlow API",
        Version = "v1",
        Description = "API quản lý hệ thống bán đồ uống BranchFlow."
    });

    // Thêm nút Authorize để nhập JWT trong Swagger
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Nhập access token, không cần thêm chữ Bearer"
    });

    // Yêu cầu Swagger gửi JWT trong Authorization header
    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document, null)] = new List<string>()
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

// Đọc và xác thực JWT
app.UseAuthentication();

// Kiểm tra quyền
app.UseAuthorization();

// Ánh xạ các route được khai báo bằng attribute trong controller.
app.MapControllers();

app.Run();
