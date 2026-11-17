# Sửa hồ sơ nhân viên trên Web

## Phạm vi

- ADMIN mở form từ danh sách hoặc hồ sơ nhân viên trong chi nhánh được phân công.
- OWNER chỉ xem. EMPLOYEE không có quyền đọc/ghi màn quản lý này.
- Sửa họ tên, ngày sinh, điện thoại, email, địa chỉ, mã nhân viên, ngày vào làm, chức vụ và lương cơ bản qua PUT hiện có.
- Không sửa username/password, trạng thái làm việc hoặc phân công chi nhánh trong mốc này. Không đổi schema, không làm CR-001.

## Flow và file chính

`EmployeeEdit.razor` -> `EmployeeApiService` -> `PUT /api/employees/{id}?branchId=...` -> `EmployeeService.UpdateEmployeeAsync` -> repositories/UnitOfWork -> SQL.

- Model form dùng `EmployeeUpdateDTO`; dữ liệu được sao chép từ hồ sơ vào model riêng.
- API vẫn kiểm tra ADMIN và phạm vi chi nhánh khi lưu; không tin route hoặc nút ẩn ở Web.
- `EmployeeDetailApiResult` giữ HTTP status và lỗi từng trường từ ValidationProblemDetails.
- `EmployeeEditTextInput`, `EmployeeEditTextArea`, `EmployeeEditSalaryInput` kế thừa input chuẩn Blazor, cập nhật khi gõ và giữ validation/EditContext; tránh chờ blur mới nhận biết dữ liệu thay đổi.
- Khi lưu thành công, form dùng DTO backend trả về và trở lại trạng thái chưa thay đổi. Không suy đoán thành công từ dữ liệu cũ.

## Điều chỉnh UI/UX

- Nút sửa ở cả danh sách và hồ sơ, chỉ hiển thị cho ADMIN.
- Form chia hai nhóm: cá nhân/liên hệ và công việc; hiển thị ngữ cảnh chi nhánh và trạng thái chỉ đọc.
- Một cột trên điện thoại, hai cột trường phù hợp trên desktop; control tối thiểu 44px; Lưu/Hủy ở thanh sticky phía dưới.
- Nhận biết thay đổi ngay khi gõ, khóa gửi trùng, focus vùng thông báo sau lỗi/lưu thành công.
- Lỗi email/mã trùng hiển thị cạnh trường và giữ dữ liệu nhập. Kiểm tra ngày tương lai và lương theo decimal(18,2) dùng chung ở DTO cho Web/API; giới hạn decimal đọc theo invariant culture.
- Cảnh báo native trước khi rời trang có thay đổi chưa lưu. Nếu mất kết nối hoặc phản hồi lưu không đọc được, không tự gửi lại; yêu cầu tải lại để xác định kết quả.
- Danh sách hỗ trợ Enter để tìm; không che toàn bộ danh sách khi lọc; khóa điều khiển trong lúc tìm, giữ bộ lọc khi rỗng/lỗi và ghi rõ số liệu theo bộ lọc hiện tại.
- Hồ sơ mobile bỏ khối tóm tắt trùng lặp; thông tin chính và lịch sử vẫn còn. Nút sửa rộng và dễ chạm; giữ Be Vietnam Pro, không thêm emoji/vector trang trí.

## Kiểm chứng

- Release build: 0 warning/error. Unit test bổ sung cho Web HTTP client, validation ngày/lương và culture; toàn bộ 140 test đạt tại mốc kiểm chứng.
- Browser ADMIN: đi từ hồ sơ sang sửa; nhập dữ liệu; chặn email không hợp lệ; lỗi email/mã trùng giữ dữ liệu; lưu họ tên và lương; hủy điều hướng giữ dữ liệu và xác nhận rời trang bỏ thay đổi.
- Đối chiếu SQL local: họ tên AppUser và BaseSalary EmployeeProfile đã cập nhật; lịch sử phân công không đổi.
- HTTP thật: OWNER và EMPLOYEE PUT trả 403; branch không tồn tại trả 404; ngày vào làm tương lai trả 400; mã trùng trả đúng 409 kèm lỗi trường.
- OWNER trên browser đọc hồ sơ không có nút sửa; URL sửa trực tiếp chỉ hiện thông báo không có quyền, không render form.
- Browser danh sách: Enter tìm đúng fixture, tìm không có kết quả vẫn giữ bộ lọc. Đã quan sát desktop 1280px/mobile 390px; đo thêm 320px không tràn ngang.
- Fixture chỉ nằm ở SQL localhost, có prefix riêng; đã dọn toàn bộ nhân viên thử. Không thao tác database cloud. Ảnh local nằm trong `artifacts/screenshots/employee-edit-desktop.jpg`.

## Giới hạn và bước sau

- Giữ API cập nhật hiện có, chưa thêm rowversion/ETag để chống hai ADMIN sửa cùng một hồ sơ. Không coi đây là bảo đảm chống ghi đè đồng thời.
- Sửa HireDate không tự viết lại UserBranch; quy tắc điều chỉnh lịch sử hiệu lực sâu hơn cần chốt riêng.
- Chọn ngày dùng control native của trình duyệt/thiết bị; định dạng lịch hiển thị phụ thuộc ngôn ngữ thiết bị.
- Chưa đổi trạng thái làm việc hoặc chuyển chi nhánh trên Web; đó là hai bước tiếp theo.
- Người dùng đã nghiệm thu ngày 2026-11-17 và yêu cầu commit/push. Chưa deploy cho mốc này.
