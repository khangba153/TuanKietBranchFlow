# TuanKietBranchFlow

TuanKietBranchFlow là hệ thống quản lý vận hành chuỗi cửa hàng đồ uống, được xây dựng dưới dạng đồ án cá nhân với ASP.NET Core Web API, Entity Framework Core, SQL Server và Blazor Web App.

## Chức năng đang phát triển

- Đăng nhập bằng JWT và phân quyền `OWNER`, `ADMIN`, `EMPLOYEE`.
- Kiểm soát quyền truy cập theo chi nhánh được phân công.
- Quản lý danh sách, hồ sơ, trạng thái và lịch sử chuyển chi nhánh của nhân viên.
- Giao diện Blazor responsive gọi dữ liệu qua Web API.

## Công nghệ

- .NET 10 và ASP.NET Core Web API
- Blazor Web App với Interactive Server
- Entity Framework Core theo hướng Database First
- SQL Server
- Swagger
- Bootstrap

## Cấu trúc solution

```text
TuanKietBranchFlow/
|-- TuanKietBranchFlow.Api/
|-- TuanKietBranchFlow.Application/
|-- TuanKietBranchFlow.Infrastructure/
|-- TuanKietBranchFlow.Web/
|-- database/
|-- docs/
|-- .config/
|-- TuanKietBranchFlow.slnx
|-- README.md
`-- .gitignore
```

- `Api`: controller, xác thực JWT và cấu hình dependency injection.
- `Application`: DTO, interface và xử lý nghiệp vụ.
- `Infrastructure`: EF Core model, DbContext, repository và Unit of Work.
- `Web`: giao diện Blazor và lớp gọi Web API.
- `database`: script tạo cơ sở dữ liệu.
- `docs`: tài liệu nghiệp vụ, thiết kế database, roadmap và prototype giao diện.
- `.config`: manifest công cụ .NET dùng chung cho repository.

## Chuẩn bị môi trường

- .NET SDK 10
- SQL Server 2019 trở lên hoặc SQL Server chạy bằng Docker
- Git

Khôi phục package và công cụ:

```powershell
dotnet restore .\TuanKietBranchFlow.slnx
dotnet tool restore
```

Tạo database bằng script:

```text
database/BranchFlowDB.sql
```

Thiết lập secret cho API. Không commit connection string hoặc JWT key vào Git:

```powershell
dotnet user-secrets set "ConnectionStrings:BranchFlowDatabase" "<connection-string>" --project .\TuanKietBranchFlow.Api\TuanKietBranchFlow.Api.csproj
dotnet user-secrets set "Jwt:Key" "<jwt-secret-key>" --project .\TuanKietBranchFlow.Api\TuanKietBranchFlow.Api.csproj
```

## Chạy local

Mở hai terminal tại thư mục gốc.

Terminal API:

```powershell
dotnet run --project .\TuanKietBranchFlow.Api\TuanKietBranchFlow.Api.csproj
```

Terminal Web:

```powershell
dotnet run --project .\TuanKietBranchFlow.Web\TuanKietBranchFlow.Web.csproj
```

Địa chỉ mặc định khi dùng profile HTTP:

- Swagger: `http://localhost:5007/swagger`
- Web: `http://localhost:5032`

## Build

```powershell
dotnet build .\TuanKietBranchFlow.slnx
```

## Trạng thái

Dự án đang được phát triển theo từng vertical slice. Các flow nhân viên đã được kiểm tra thủ công trên môi trường local; automated test, CI/CD và bản deploy public sẽ được bổ sung ở các mốc tiếp theo.
