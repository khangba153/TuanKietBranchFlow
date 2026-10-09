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
using System.Diagnostics;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Mvc;

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

// Repository quản lý các phiên đăng nhập
builder.Services.AddScoped<IAuthSessionRepository, AuthSessionRepository>();

// Repository quản lý refresh token
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();

// Đăng ký repository để truy vấn chi nhánh
builder.Services.AddScoped<IBranchRepository, BranchRepository>();

// Đăng ký repository để truy vấn danh sách Role
builder.Services.AddScoped<IRoleRepository, RoleRepository>();

// Đăng ký repository để truy vấn danh sách nhân viên
builder.Services.AddScoped<IEmployeeRepository, EmployeeRepository>();

// Đăng ký repository để thêm và quản lý phân công chi nhánh
builder.Services.AddScoped<IUserBranchRepository, UserBranchRepository>();

// Đăng ký respository để đọc menu gọi món
builder.Services.AddScoped<IOrderMenuRepository, OrderMenuRepository>();

// Đăng ký repository để kiểm tra dữ liệu và lưu đơn hàng
builder.Services.AddScoped<IOrderRepository, OrderRepository>();

// Đăng ký repository để đọc danh mục quản lý
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();

// Đăng ký repository để đọc size quản lý
builder.Services.AddScoped<ISizeRepository, SizeRepository>();

// Đăng ký repository để đọc món quản lý
builder.Services.AddScoped<IProductRepository, ProductRepository>();

// Đăng ký repository để đọc nhóm topping và topping quản lý
builder.Services.AddScoped<IToppingGroupRepository, ToppingGroupRepository>();

// Repository xử lý truy vấn và thêm topping
builder.Services.AddScoped<IToppingRepository, ToppingRepository>();

// Repository cho nhóm ghi chú và lựa chọn con dùng chung
builder.Services.AddScoped<INoteGroupRepository, NoteGroupRepository>();
builder.Services.AddScoped<INoteOptionRepository, NoteOptionRepository>();

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

// Đăng ký service xử lý nghiệp vụ nhân viên
builder.Services.AddScoped<IEmployeeService, EmployeeService>();

// Đăng ký service xử lý nghiệp vụ gọi món
builder.Services.AddScoped<IOrderMenuService, OrderMenuService>();

// Đăng ký service xử lý nghiệp vụ tạo đơn hàng
builder.Services.AddScoped<IOrderService, OrderService>();

// Đăng ký service xử lý danh mục quản lý
builder.Services.AddScoped<ICategoryService, CategoryService>();

// Đăng ký service xử lý lấy size quản lý
builder.Services.AddScoped<ISizeService, SizeService>();

// Đăng ký service xử lý danh sách món quản lý
builder.Services.AddScoped<IProductService, ProductService>();

// Đăng ký service xử lý nhóm topping và topping quản lý
builder.Services.AddScoped<IToppingGroupService, ToppingGroupService>();

// Service xử lý nghiệp vụ topping
builder.Services.AddScoped<IToppingService, ToppingService>();

// Service nghiệp vụ quản lý ghi chú nhanh
builder.Services.AddScoped<INoteGroupService, NoteGroupService>();
builder.Services.AddScoped<INoteOptionService, NoteOptionService>();

// Đăng ký UnitOfWork để các service có 1 điểm lưu dữ liệu thống nhất
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Đăng ký service tạo JWT từ cấu hình của API
builder.Services.AddScoped<JwtTokenService>(servicePorvider =>
{
    return new JwtTokenService(jwtKey, jwtIssuer, jwtAudience, jwtExpireMinutes);
});

// Đăng ký controller, model binding và chuyển đổi dữ liệu JSON
builder.Services.AddControllers();

// Giới hạn tổng request đăng nhập trên một instance API.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode =
        StatusCodes.Status429TooManyRequests;

    // Trả lỗi chuẩn khi request đăng nhập vượt giới hạn.
    options.OnRejected = async (context, cancellationToken) =>
    {
        HttpContext httpContext = context.HttpContext;

        httpContext.Response.StatusCode =
            StatusCodes.Status429TooManyRequests;

        httpContext.Response.Headers.CacheControl = "no-store";

        // Dùng dịch vụ lỗi đã đăng ký bằng AddProblemDetails.
        IProblemDetailsService problemDetailsService =
            httpContext.RequestServices
                .GetRequiredService<IProblemDetailsService>();

        bool isWritten = await problemDetailsService.TryWriteAsync(
            new ProblemDetailsContext
            {
                HttpContext = httpContext,
                ProblemDetails = new ProblemDetails
                {
                    Status = StatusCodes.Status429TooManyRequests,
                    Title = "Quá nhiều yêu cầu đăng nhập",
                    Detail = "Vui lòng chờ khoảng 1 phút rồi thử lại."
                }
            });

        // Vẫn trả thông báo nếu client không nhận định dạng ProblemDetails.
        if (!isWritten)
        {
            httpContext.Response.ContentType = "text/plain; charset=utf-8";

            await httpContext.Response.WriteAsync(
                "Quá nhiều yêu cầu đăng nhập. Vui lòng thử lại sau.",
                cancellationToken);
        }
    };

    options.AddFixedWindowLimiter("ApiLogin", limiterOptions =>
    {
        limiterOptions.PermitLimit = 30;
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.QueueLimit = 0;
        limiterOptions.AutoReplenishment = true;
    });
});

// Đăng ký health check cơ bản, chưa kiểm tra kết nối database
builder.Services.AddHealthChecks();

// Đăng ký response lỗi chuẩn, không đưa chi tiết exception ra client
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        // Mã dùng để đổi chiếu request lỗi với log phía server
        string traceId = Activity.Current?.Id
            ?? context.HttpContext.TraceIdentifier;

        context.ProblemDetails.Extensions["traceId"] = traceId;

        // Chỉ dùng thông báo chung cho lỗi nội bộ, không đổi nghiệp vụ
        if (context.ProblemDetails.Status
            == StatusCodes.Status500InternalServerError)
        {
            context.ProblemDetails.Title = "Lỗi hệ thống";
            context.ProblemDetails.Detail = "Đã xảy ra lỗi trong quá trình xử lý. Vui lòng thử lại sau.";
        }
    };
});

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

    // Kiểm tra phiên sau khi JWT đã vượt qua xác thực cơ bản
    options.Events = new JwtBearerEvents
    {
        OnTokenValidated = async context =>
        {
            // Đọc claim từ JWT đã được xác thực
            string? sessionIdValue =
                context.Principal?.FindFirstValue("sid")
                ?? context.Principal?.FindFirstValue(ClaimTypes.Sid);

            string? userIdValue =
                context.Principal?.FindFirstValue(
                    ClaimTypes.NameIdentifier);

            string? roleCode =
                context.Principal?.FindFirstValue(ClaimTypes.Role);

            bool isValidSessionId =
                Guid.TryParse(sessionIdValue, out Guid sessionId);

            bool isValidUserId =
                int.TryParse(userIdValue, out int userId);

            if (!isValidSessionId ||
                !isValidUserId ||
                string.IsNullOrWhiteSpace(roleCode))
            {
                context.Fail("Token không chứa thông tin phiên hợp lệ.");
                return;
            }

            // Lấy Service trong scope của request hiện tại
            IAuthService authService =
                context.HttpContext.RequestServices
                    .GetRequiredService<IAuthService>();

            bool isSessionValid =
                await authService.IsSessionValidAsync(
                    sessionId,
                    userId,
                    roleCode);

            if (!isSessionValid)
            {
                context.Fail("Phiên đăng nhập không còn hợp lệ.");
                return;
            }
        }
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
    // Xác định đường dẫn file XML được sinh ra khi build APi
    string xmlFileName = $"{builder.Environment.ApplicationName}.xml";
    string xmlFilePath = Path.Combine(AppContext.BaseDirectory, xmlFileName);
    // Cho Swagger đọc nội dung summary từ file XML
    options.IncludeXmlComments(xmlFilePath);

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

// Bắt exception ngoài dự kiến trong môi trường không phải Development
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler();
}

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

// Xác định endpoint để đọc policy giới hạn request.
app.UseRouting();

// Đọc và xác thực JWT
app.UseAuthentication();

// Kiểm tra quyền
app.UseAuthorization();

// Áp dụng giới hạn request cho endpoint có gắn policy.
app.UseRateLimiter();

// Ánh xạ các route được khai báo bằng attribute trong controller.
app.MapControllers();

// Cho phép kiểm tra API có phản hồi mà không cần đăng nhập
app.MapHealthChecks("/health").AllowAnonymous();

app.Run();
