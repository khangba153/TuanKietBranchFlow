# Điểm dừng học tập BranchFlow

Cập nhật: 2026-09-07. Đây là ghi chú tiếp nối, không thay thế việc đọc code mới nhất.

## Chủ đề hiện tại

Đang nối giao diện Blazor vào vertical slice xác thực. Login, authentication state, khôi phục sau F5 và logout phía Web đã chạy; bước kế tiếp là tự động gắn access token vào request API được bảo vệ.

## Mốc vừa hoàn thành

- Đã có các API nhân viên: danh sách, chi tiết, tạo, cập nhật, chuyển chi nhánh và đổi trạng thái.
- `PATCH /api/employees/{employeeId}/status?branchId=...` cập nhật thống nhất `AppUser`, `EmployeeProfile` và `UserBranch`, sau đó lưu một lần qua Unit of Work.
- Khi cho nghỉ, `LeaveDate` là ngày bắt đầu nghỉ và `ActiveTo = LeaveDate - 1 ngày`.
- Service đã chặn ngày nghỉ không hợp lệ để tránh vi phạm ràng buộc `ActiveFrom <= ActiveTo` trong database.
- Truy vấn danh sách và chi tiết đều xem được nhân viên đang làm hoặc nhân viên đã nghỉ tại đúng chi nhánh phát sinh việc nghỉ.
- Agent đã build solution bằng thư mục output tạm: 0 warning, 0 error. Build thông thường từng bị khóa DLL do API đang chạy; đó không phải lỗi code.
- Người học báo đã test thủ công bằng Swagger và mọi kết quả đúng dự đoán, gồm danh sách, chi tiết, đổi trạng thái và phạm vi chi nhánh. Đây chưa phải automated test hoặc kiểm thử production.
- `Login.razor` đã gọi `AuthApiService`, xử lý validation/loading/thông báo và build Web đạt 0 warning, 0 error.
- Đã cài `Blazor.LocalStorage 10.1.0`, đăng ký `ILocalStorageService` và lưu access token với key `accessToken` sau khi đăng nhập thành công.
- Người học báo đã test trên trình duyệt: đăng nhập thành công, token xuất hiện trong LocalStorage và vẫn còn sau F5. Đây là test thủ công, chưa chứng minh token đã được gắn vào request API khác.
- `AuthApiService.GetCurrentUserAsync()` đã gắn `Authorization: Bearer`, gọi `GET /api/auth/me` và build Web đạt 0 warning, 0 error.
- Người học báo đã test runtime thành công: `Login.razor` đọc lại token, `/api/auth/me` trả `CurrentUserDTO`, giao diện hiển thị đúng username và role. Đây là test thủ công trên local, chưa phải automated test.
- Đã tạo `CustomAuthenticationStateProvider`, đăng ký cùng một scoped instance cho kiểu cụ thể và `AuthenticationStateProvider`, đồng thời chuyển `Routes` sang Interactive Server toàn cục.
- `Login.razor` gọi `MarkUserAsAuthenticated()` sau khi `/api/auth/me` thành công; `MainLayout` dùng `AuthorizeView` để hiện username hoặc nút đăng nhập.
- Web đã build sau thay đổi authentication state: 0 warning, 0 error. Người học báo đã test thủ công: sau login layout hiện username; sau F5 layout trở về anonymous trong khi token vẫn còn LocalStorage, đúng với trạng thái hiện tại chưa có restore.
- `MainLayout.OnAfterRenderAsync(firstRender)` đã gọi provider đọc token từ LocalStorage, gọi `/api/auth/me` và tái tạo `ClaimsPrincipal` khi token hợp lệ.
- Web build sau phần restore đạt 0 warning, 0 error. Người học báo đã test thủ công đủ ba trường hợp: token hợp lệ giữ username sau F5; không có token hiện nút đăng nhập; token giả bị API từ chối, bị xóa và giao diện trở về anonymous.
- Provider đã có `LogoutAsync()` để xóa access token, tạo `ClaimsPrincipal` anonymous và thông báo lại authentication state. `MainLayout` hiện username/nút logout khi authorized và nút login khi anonymous.
- Web build sau phần logout đạt 0 warning, 0 error. Người học báo đã test thủ công: logout chuyển về `/login`, token bị xóa khỏi LocalStorage và F5 vẫn giữ trạng thái anonymous.

## Kiến thức người học đã trình bày đạt

- `PATCH` phù hợp vì endpoint chỉ đổi trạng thái, không cập nhật toàn bộ hồ sơ.
- Nghỉ việc không phải soft delete: vẫn giữ hồ sơ và lịch sử phân công.
- Một phân công kết thúc chưa đủ kết luận nhân viên nghỉ; phải kết hợp `AppUser.IsActive`, `EmployeeProfile.LeaveDate` và `UserBranch.ActiveTo`.
- `Include/ThenInclude` tải dữ liệu liên quan để ánh xạ DTO; `Any` dùng làm điều kiện lọc trong truy vấn SQL.
- Lỗi dữ liệu nghiệp vụ nên được kiểm tra để trả 400/409 phù hợp, không chờ database ném lỗi thành 500.
- Flow đã hiểu: JWT middleware -> Controller -> Service -> Repository/Unit of Work -> DbContext -> SQL -> DTO -> HTTP response.

## Quyết định nghiệp vụ đang giữ

- `EffectiveDate` khi nghỉ là ngày đầu tiên nhân viên nghỉ.
- Khi nghỉ: `EmployeeProfile.LeaveDate = EffectiveDate` và phân công hiện tại có `ActiveTo = EffectiveDate.AddDays(-1)`.
- Ngày nghỉ sớm nhất phải sau `ActiveFrom`; nếu bắt đầu làm ngày 10/09 thì làm hết ngày 10/09 và sớm nhất nghỉ từ 11/09.
- Khi hoạt động lại: xóa `LeaveDate`, bật `AppUser.IsActive` và tạo một `UserBranch` mới; không sửa mất lịch sử cũ.
- ADMIN chỉ thao tác trong chi nhánh còn được phân công; nhân viên nghỉ được xem tại chi nhánh nơi phát sinh lần nghỉ đó.

## Bước kế tiếp duy nhất

Thiết kế bước tự động gắn `Authorization: Bearer <token>` cho các HttpClient gọi endpoint được bảo vệ. Giải thích `DelegatingHandler` và chỉ triển khai sau khi người học phân biệt được handler phía Web với JWT middleware phía API.

## Việc cần trước khi public production

- JWT đã cấp chưa được chứng minh bị vô hiệu ngay khi `AppUser.IsActive` chuyển thành false; cần chốt chiến lược kiểm tra hoặc thu hồi token.
- Chưa có automated test cho các luật ngày hiệu lực và phạm vi chi nhánh.
- Chưa có CI chạy restore/build/test trên máy độc lập.
- Web đã có authentication state, khôi phục sau F5 và logout phía Web, nhưng chưa tự động gắn token cho các API khác.
- Chưa có xử lý lỗi production tập trung, health check, log có mã truy vết, cấu hình cloud database hoặc bằng chứng deploy public.
