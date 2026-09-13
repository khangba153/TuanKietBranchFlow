# Điểm dừng học tập BranchFlow

Cập nhật: 2026-09-13. Đây là ghi chú tiếp nối, không thay thế việc đọc code mới nhất.

## Chủ đề hiện tại

CI build đã chạy xanh. Người học đã chạy API và Web publish trong Production trên local và báo test giao diện thành công; đang củng cố cấu hình Web trước khi học nhóm automated test đầu tiên. Chưa deploy cloud/public.

## Mốc vừa hoàn thành

- Đã tạo `TuanKietBranchFlow.Tests` dùng xUnit, target `net10.0`, tham chiếu Application và được thêm vào solution. Agent đã chạy `dotnet test` Release: 1 bài mẫu rỗng Passed, 0 Failed. Đây chỉ chứng minh test runner hoạt động, chưa có bài kiểm tra nghiệp vụ thật.

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
- Thử nghiệm `ApiAuthorizationHandler` build xanh nhưng browser test tái hiện lỗi runtime: handler do `IHttpClientFactory` tạo trong scope riêng, nên `ILocalStorageService` không dùng được JavaScript runtime của circuit và ném `InvalidOperationException`. API đo trực tiếp vẫn nhanh (`/login` khoảng 48 ms và `/me` khoảng 3 ms sau warm-up); cảm giác login lâu là do circuit chờ rồi bị ngắt, không phải SQL/JWT chậm.
- Đã thay handler bằng `AuthorizedApiService` chạy trong Blazor circuit, gỡ `ApiAuthorizationHandler` và client pipeline lỗi. Web build đạt 0 warning, 0 error.
- Agent đã chạy browser test thật bằng Playwright trên bản build mới: login hợp lệ chuyển về trang chủ trong khoảng 292 ms, F5 vẫn hiện `admin01`, console không có lỗi và logout xóa sạch LocalStorage. Đây là test tự động hóa local cho flow UI, chưa phải test source được commit vào repository.
- Người học đã restart Web chính ở cổng `5032` và báo test thủ công đúng dự đoán: login nhanh, restore sau F5 và logout đều hoạt động với cấu hình DI mới.
- Trang danh sách nhân viên đã nhận `branchId`, tìm theo từ khóa và lọc đủ ba trạng thái. Người học báo đã test thủ công thành công sau khi giao diện dùng chuỗi cho giá trị `<select>` rồi chuyển thành `bool?` trước khi gọi API.
- Trang hồ sơ nhân viên đã đi hết flow từ nút Hồ sơ ở danh sách đến `GET /api/employees/{employeeId}?branchId=...`. Agent đã kiểm tra trên trình duyệt local: hồ sơ nhân viên nghỉ hiển thị đầy đủ thông tin, ngày nghỉ và lịch sử chi nhánh; Web build đạt 0 warning, 0 error.
- Người học báo đã test thủ công thêm hồ sơ nhân viên đang làm và ID không tồn tại, kết quả đúng dự đoán. Vertical slice đọc nhân viên đã hoàn thành ở mức local/manual, chưa có automated test lưu trong source.
- API tạo nhân viên đã chặn `HireDate` trong tương lai tại `EmployeeService` và ánh xạ thành HTTP 400 tại `EmployeesController`; solution build 0 warning, 0 error. Người học báo đã test thủ công Swagger: ngày tương lai trả 400, ngày hợp lệ tạo thành công.
- `EmployeeCreate.razor` đã dùng chuỗi `dd/MM/yyyy`, `DateOnly.TryParseExact` và kiểm tra ngày tương lai trước khi gọi API; Web build 0 warning, 0 error. Người học báo đã test thủ công đủ ngày hợp lệ, sai định dạng, ngày không tồn tại, ngày tương lai và sửa lại sau lỗi, kết quả đúng dự đoán.
- Web đã có `EmployeeCreateApiResult`; `EmployeeApiService` đọc `ProblemDetails`, status code và dữ liệu thành công thay vì đổi mọi lỗi thành `null`. Web build 0 warning, 0 error. Người học báo đã test thủ công các lỗi 401/403/404/409 và trường hợp tạo thành công, kết quả đúng dự đoán.
- Commit `a64f184` đã thêm `.github/workflows/ci.yml`. Ảnh GitHub Actions do người học cung cấp cho thấy lần chạy đầu tiên của `BranchFlow CI` đã hoàn thành màu xanh: checkout source, cài .NET SDK, restore NuGet và build solution ở cấu hình Release đều thành công. Đây là CI build thật trên runner độc lập, chưa có automated test, chạy ứng dụng hoặc deploy.
- API và Web đã được publish local vào `artifacts/`. Ảnh terminal do người học cung cấp cho thấy bản `TuanKietBranchFlow.Api.dll` khởi động thành công ngoài Visual Studio tại `http://localhost:5107`, môi trường `Production`, với content root đúng là `artifacts/api`. Đây mới chứng minh startup và Kestrel lắng nghe; chưa chứng minh request login, kết nối database hoặc toàn bộ vertical slice runtime.

## Kiến thức người học đã trình bày đạt

- Người học báo smoke test API publish: login trả 200 và có token; `/api/auth/me` trả 200 với username/role đúng. Sau khi bật SQL Docker, request login hoạt động; đây là test thủ công local, không phải agent trực tiếp chạy test.
- Người học báo đã chạy Web publish Production tại cổng 5207, gọi API publish tại 5107, test toàn bộ chức năng hiện có trên giao diện và không thấy lỗi log. Đây là bằng chứng người học báo test thủ công vertical slice local, chưa phải deploy public hoặc automated regression test.
- Đã hiểu Content Root quyết định nơi đọc cấu hình, đăng ký DbContext chưa mở kết nối SQL, và login tạo token khác với middleware xác thực Bearer token.

- `PATCH` phù hợp vì endpoint chỉ đổi trạng thái, không cập nhật toàn bộ hồ sơ.
- Nghỉ việc không phải soft delete: vẫn giữ hồ sơ và lịch sử phân công.
- Một phân công kết thúc chưa đủ kết luận nhân viên nghỉ; phải kết hợp `AppUser.IsActive`, `EmployeeProfile.LeaveDate` và `UserBranch.ActiveTo`.
- `Include/ThenInclude` tải dữ liệu liên quan để ánh xạ DTO; `Any` dùng làm điều kiện lọc trong truy vấn SQL.
- Lỗi dữ liệu nghiệp vụ nên được kiểm tra để trả 400/409 phù hợp, không chờ database ném lỗi thành 500.
- Flow đã hiểu: JWT middleware -> Controller -> Service -> Repository/Unit of Work -> DbContext -> SQL -> DTO -> HTTP response.
- Đã phân biệt trigger `on.push.branches` với `actions/checkout`: trigger quyết định lúc workflow chạy, còn checkout tải source của commit tương ứng xuống runner.
- Đã phân biệt CI build xanh với runtime/deploy/production: build xanh chỉ chứng minh source đã commit có thể checkout, restore và compile Release trên runner độc lập.

## Quyết định nghiệp vụ đang giữ

- `EffectiveDate` khi nghỉ là ngày đầu tiên nhân viên nghỉ.
- Khi nghỉ: `EmployeeProfile.LeaveDate = EffectiveDate` và phân công hiện tại có `ActiveTo = EffectiveDate.AddDays(-1)`.
- Ngày nghỉ sớm nhất phải sau `ActiveFrom`; nếu bắt đầu làm ngày 10/09 thì làm hết ngày 10/09 và sớm nhất nghỉ từ 11/09.
- Khi hoạt động lại: xóa `LeaveDate`, bật `AppUser.IsActive` và tạo một `UserBranch` mới; không sửa mất lịch sử cũ.
- ADMIN chỉ thao tác trong chi nhánh còn được phân công; nhân viên nghỉ được xem tại chi nhánh nơi phát sinh lần nghỉ đó.

## Bước kế tiếp duy nhất

Củng cố việc Web Production đọc `ApiSettings__BaseUrl`, gọi API qua HttpClient và phân biệt Production mode local với deploy public. Sau khi chấm câu trả lời, giải thích automated test từ nền tảng và chọn một nhóm test nhỏ cho nghiệp vụ nhân viên; chưa tự tạo test project hoặc CD khi người học chưa hiểu.

## Việc cần trước khi public production

- JWT đã cấp chưa được chứng minh bị vô hiệu ngay khi `AppUser.IsActive` chuyển thành false; cần chốt chiến lược kiểm tra hoặc thu hồi token.
- Chưa có automated test cho các luật ngày hiệu lực và phạm vi chi nhánh.
- CI đã chạy restore/build Release trên GitHub runner độc lập, nhưng chưa có project automated test nên pipeline chưa chạy test.
- Web đã có authentication state, khôi phục sau F5, logout và `AuthorizedApiService` gắn Bearer token trong đúng circuit. Chưa có xử lý lỗi tập trung khi API/Web mất kết nối và chưa có automated test được lưu trong source.
- Chưa có xử lý lỗi production tập trung, health check, log có mã truy vết, cấu hình cloud database hoặc bằng chứng deploy public.
