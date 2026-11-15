# Dependency audit — 2026-11-15

## Phát hiện và xử lý

`dotnet list TuanKietBranchFlow.slnx package --vulnerable --include-transitive --no-restore`
phát hiện System.Security.Cryptography.Pkcs 9.0.11 trong cả năm project.
Chuỗi dependency: EF Core SqlServer 10.0.11 → Microsoft.Data.SqlClient 6.1.6 → Pkcs 9.0.11.

Infrastructure ghim chính dependency đã tồn tại lên 9.0.21, không nâng major
SqlClient/EF Core hoặc thêm chức năng mật mã mới. Đây là bản vá cùng dòng 9.x
được Microsoft liệt kê cho CVE-2026-84008 và CVE-2026-70162.

Nguồn chính thức:

- [Microsoft CVE-2026-84008](https://github.com/dotnet/announcements/issues/468)
- [Microsoft CVE-2026-70162](https://github.com/dotnet/announcements/issues/454)
- [NuGet Pkcs 9.0.21](https://www.nuget.org/packages/System.Security.Cryptography.Pkcs/9.0.21)

## Kiểm chứng

- Restore solution thành công.
- Audit sau restore không tìm thấy package có lỗ hổng theo nguồn NuGet hiện tại.
- Không suppress NU1902/NU1903 hoặc vô hiệu hóa NuGet audit.
- Build/test và smoke test SQL/login được chạy cùng đợt khép B4.1, xem báo cáo chức năng.

Audit này không chứng minh ứng dụng an toàn tuyệt đối, không thay thế rà soát
code/cấu hình, và chưa xác nhận package/runtime của bản đang chạy trên Azure.
Không tự nâng SDK/Visual Studio/App Service runtime hoặc deploy cloud trong đợt này.
