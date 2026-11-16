# Employee xem kết quả xử lý và báo sai lại

## Phạm vi đã duyệt

- Employee chỉ đọc đơn do chính mình tạo.
- Sau mỗi lần admin xử lý, employee xem được kết quả, người xử lý, thời gian UTC, lý do và nội dung món/tổng tiền trước–sau.
- `COMPLETED` và `ADJUSTED` được báo sai lại. `NEEDS_REVIEW` và `CANCELLED` không được gửi.
- Khi đã có lịch sử xử lý, employee phải xác nhận đã xem kết quả gần nhất và nhập lý do mới. Không tự chặn một lý do chỉ vì trùng chữ với lần trước: lỗi có thể vẫn chưa được sửa.

## Flow

1. `MyOrderDetail` gọi `GET /api/orders/{id}` bằng tài khoản employee.
2. Repository lọc theo `CreatedByUserId`, tải lịch sử; service ánh xạ snapshot sang DTO hiển thị, không trả raw JSON audit.
3. UI hiển thị lịch sử gần nhất, so sánh trước/sau, nút `Báo sai lại` và checkbox xác nhận.
4. Request báo sai gửi `Reason`, `ExpectedHistoryId`, `HasReviewedResolution`.
5. Service kiểm tra quyền sở hữu, trạng thái, phiên bản và xác nhận; repository thực hiện một `UPDATE` có điều kiện tại SQL.
6. Nếu trạng thái hoặc lịch sử đã đổi trước lúc ghi, trả 409 và yêu cầu tải lại. Hai request đồng thời chỉ một request được nhận.

## Cập nhật trang đang mở

- Trang chi tiết kiểm tra lại mỗi 15 giây, không dùng WebSocket/SignalR mới và không cần đổi schema.
- Khi nhận trạng thái/lịch sử mới: cập nhật dữ liệu, hiện thông báo và bỏ xác nhận đã đọc bản cũ.
- Giữ lý do employee đang nhập; không ghi đè dữ liệu mới bằng response tải cũ đã bị vượt phiên bản.
- Có nút `Tải lại`. Khi gửi gặp lỗi bất định, không tự gửi lại; yêu cầu tải lại để xem kết quả thực tế.
- Dừng vòng kiểm tra khi rời component. Đây là polling, không phải thông báo tức thời.

## File chính

- Application: `MyOrderResolutionDTO`, `MyOrderDetailDTO`, `ReportOrderRequestDTO`, `OrderService`.
- Infrastructure: `IOrderRepository`, `OrderRepository`.
- Web: `MyOrderDetail`, `MyOrderResolutionHistory`, `MyOrderResolutionItems`.
- Tests: `EmployeeOrderResolutionTests`, regression test báo sai trong `OrderServiceTests`.

## Kiểm chứng local

- Build Release: không lỗi/cảnh báo; 123/123 unit test đạt.
- SQL local: gửi lần đầu; giữ nguyên rồi báo lại; đổi size rồi đọc trước/sau và báo lại; chặn bản cũ/chưa xác nhận; cô lập quyền sở hữu; hai request đồng thời chỉ một request thành công.
- Browser local: employee đọc bản điều chỉnh; nhập lý do nhưng chưa xác nhận thì nút gửi bị khóa; xác nhận rồi gửi thành công; admin giữ nguyên qua vòng xử lý tiếp theo, trang tự cập nhật và bỏ chọn xác nhận cũ mà không F5. Console không có lỗi.
- Đã quan sát desktop 1280px và mobile 390px; đo thêm 320px không tràn ngang. Ảnh kiểm chứng local: `artifacts/screenshots/employee-resolution-desktop.jpg`.
- Probe nằm trong `artifacts/employee-resolution-probe`, từ chối connection không trỏ localhost; dữ liệu fixture có prefix riêng và được dọn sau kiểm thử.
- Chưa deploy Azure; không thay đổi schema hoặc dữ liệu cloud.
