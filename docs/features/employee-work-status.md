# Trạng thái làm việc và thu hồi phiên nhân viên

## Phạm vi đã duyệt

- ADMIN cập nhật tại chi nhánh được phân công; OWNER chỉ xem.
- Chỉ hiệu lực hôm nay, không thêm lịch nghỉ/quay lại trong tương lai hoặc hồi tố.
- Ngày nghỉ là ngày đầu tiên không làm việc: đóng phân công vào hôm trước và khóa tài khoản.
- Không cho nghỉ ngay ngày bắt đầu phân công. Không thay đổi trạng thái khi còn phân công tương lai.
- Quay lại làm tại chi nhánh nơi nghỉ, tạo phân công mới; không mở lại hoặc viết đè lịch sử cũ.
- Không đổi schema, không thêm role hoặc CR-001, chưa làm UI chuyển chi nhánh.

## Flow triển khai

`EmployeeDetail` -> `EmployeeStatusPanel` -> `EmployeeApiService.UpdateEmployeeStatusAsync`
-> `PATCH /api/employees/{id}/status?branchId=...` -> `EmployeeService.UpdateEmployeeStatusAsync`
-> repositories/UnitOfWork -> SQL.

- API kiểm tra role, phạm vi chi nhánh và quy tắc ngày bằng dữ liệu hiện tại; ẩn nút Web không thay thế phân quyền.
- Service mở transaction, khóa ghi AppUser trước khi đọc và quyết định trạng thái. Hai yêu cầu đổi trạng thái cùng tài khoản được xử lý tuần tự.
- Đọc UserId riêng rồi UPDATE AppUser theo khóa chính, tránh giữ khóa join EmployeeProfile trong lúc chờ khóa tài khoản. Kiểm thử ban đầu đã phát hiện deadlock ở cách UPDATE join và đã sửa.
- Tài khoản, phân công, AuthSession.RevokedAt và RefreshToken.RevokedAt cùng commit hoặc rollback. Giữ nguyên UsedAt và bản ghi lịch sử.
- Cả nghỉ và quay lại đều thu hồi phiên/token cũ; quay lại bắt buộc đăng nhập mới, không khôi phục phiên cũ.
- Login cũng giữ khóa AppUser đến khi lưu phiên xong, chỉ khi tài khoản còn active và UpdatedAt chưa đổi so với lúc kiểm tra mật khẩu. Nếu nghỉ việc thắng trước, login bị từ chối; nếu login thắng trước, thao tác nghỉ tiếp theo thu hồi cả phiên vừa tạo.
- DTO trả lịch sử mới đúng một lần, kể cả khi EF relationship fixup đã thêm phân công vào collection.

## UI/UX

- Khối trạng thái ngay dưới tên nhân viên, tách khỏi sửa thông tin hồ sơ.
- Hai bước chọn thao tác và xác nhận, có tên nhân viên/ngày hiệu lực/hệ quả cụ thể.
- Giải thích rõ trường hợp mới phân công hôm nay hoặc còn phân công tương lai; không đưa date picker gây hiểu nhầm có thể chọn ngày khác.
- Khóa nút và điều hướng nội bộ khi đang lưu; không tự retry PATCH khi mất phản hồi. Lỗi/conflict yêu cầu đọc lại hồ sơ trước khi thao tác tiếp.
- Thành công cập nhật badge, ngày nghỉ và lịch sử từ DTO API, không suy đoán kết quả từ dữ liệu cũ.
- Lịch sử phân biệt “Sắp tới”, “Hiện tại” theo khoảng hiệu lực, không coi mọi phân công chưa có ngày kết thúc là hiện tại.
- Giữ Be Vietnam Pro, không thêm emoji/vector trang trí. Nút mobile toàn chiều rộng, tối thiểu 44px.

## Bằng chứng kiểm chứng local — 2026-11-17

- Unit tests: service kiểm tra ngày/phạm vi/future/same-status, thu hồi trước commit, rollback khi lỗi, EF fixup; login phải giành khóa tài khoản; Web client gửi đúng PATCH/branch/Bearer/date và không retry. Toàn bộ 156 test đạt.
- SQL Server local thật: hai yêu cầu nghỉ đồng thời và hai yêu cầu quay lại đồng thời đều chỉ có một thành công/một conflict; chỉ một phân công mở; DTO không nhân đôi lịch sử.
- SQL/auth: tài khoản nghỉ không đăng nhập được; access session và refresh cũ vẫn bị từ chối sau khi quay lại; đăng nhập mới thành công.
- Tiêm lỗi SaveChanges sau khi thu hồi phiên: rollback giữ tài khoản active, phân công mở và phiên chưa thu hồi, không có trạng thái nửa chừng.
- Chạy 5 lượt login song song với nghỉ việc: sau khi hai thao tác hoàn tất không còn phiên hợp lệ của tài khoản đã nghỉ.
- HTTP API local: OWNER PATCH 403, phân công bắt đầu hôm nay 400, ADMIN nghỉ thành công 200, yêu cầu cùng trạng thái 409.
- Browser ADMIN: xác nhận, hủy không đổi trạng thái, nghỉ, quay lại, hiển thị lý do không thể cho nghỉ cùng ngày sau khi quay lại. OWNER không có khối thao tác.
- Browser desktop 1280px, mobile 390px và đo 320px: không tràn ngang; nút xác nhận/hủy cao 44px.
- Fixture prefix riêng, chỉ SQL localhost, đã dọn nhân viên/phân công/phiên thử; không thay đổi database cloud.

## Giới hạn

- Thu hồi có hiệu lực khi API kiểm tra yêu cầu tiếp theo; không chủ động đẩy trình duyệt đang mở về trang đăng nhập ngay tức thì.
- Khóa mới bảo vệ luồng đổi trạng thái và login; không tuyên bố đã chống mọi xung đột với các luồng legacy sửa hồ sơ/chuyển chi nhánh. Các luồng đó chưa dùng rowversion/ETag hoặc cùng cơ chế khóa.
- Quy tắc hôm nay vẫn dùng DateTime.Today theo môi trường chạy hiện có; chuẩn hóa múi giờ nghiệp vụ là thay đổi riêng cần chốt trước.
- Người dùng đã nghiệm thu local và duyệt commit/push ngày 2026-11-17. Chưa deploy; host Debug đang mở trước đó phải khởi động lại để nạp code mới.
