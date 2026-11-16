# B4.2a — xử lý báo sai đơn hàng

Triển khai local theo quyết định người dùng ngày 2026-11-15.
Đã kiểm chứng unit test, SQL integration và HTTP. Ngày 2026-11-16, người dùng
nghiệm thu giao diện và yêu cầu commit/push. Agent chưa tự thao tác form mới trên
trình duyệt vì browser access không khả dụng ở lần kiểm tra; không coi nghiệm thu
của người dùng là bằng chứng agent đã chạy toàn bộ checklist browser. Chưa deploy.

## Quy tắc đã triển khai

“Báo sai” là yêu cầu kiểm tra, không mặc định phải hủy đơn. Chỉ ADMIN có phân công
đang hiệu lực tại chi nhánh chưa xóa được xử lý. OWNER/EMPLOYEE không được ghi.

| Hành động | Trạng thái trước | Trạng thái sau | Món/giá/tổng tiền |
|---|---|---|---|
| KEEP — Giữ nguyên đơn | NEEDS_REVIEW | COMPLETED | Giữ nguyên |
| CANCEL — Hủy đơn | NEEDS_REVIEW | CANCELLED | Giữ dữ liệu, loại khỏi giá trị hoàn tất |

- Lý do bắt buộc, trim, 1–500 ký tự. Không giới hạn ngày nghiệp vụ xử lý.
- Không xóa đơn; không sửa món/giá/ghi chú; không xóa thông tin báo sai gốc.
- Giữ nguyên đơn sẽ đưa giá trị đơn trở lại nhóm hoàn tất theo ngày nghiệp vụ gốc.
- Không hủy trực tiếp đơn COMPLETED, không mở lại CANCELLED, không thêm ADJUSTED.
- EMPLOYEE vẫn dùng luồng báo sai hiện có. Nếu đơn giữ nguyên được báo lại, mỗi
  lần xử lý tiếp theo có một history riêng; report trước đó đã nằm trong snapshot history.
- Dùng OrderAdjustment đã có, không đổi model/DbContext/schema hoặc thêm package.

## API và chống xử lý trùng

`POST /api/order-management/{orderId}/review`, role ADMIN, body:

```json
{
  "action": "KEEP",
  "reason": "Đã kiểm tra với khách, không cần thay đổi đơn",
  "expectedReportedAt": "2026-11-15T08:00:00",
  "expectedHistoryId": 0
}
```

Hai trường expected lấy từ GET chi tiết (`reportedAt`, `latestHistoryId`), không
tự nhập hoặc tính ở client. Không dùng userId/role do client cung cấp.

| HTTP | Ý nghĩa |
|---|---|
| 200 | Đã commit cả trạng thái và history; response có status mới |
| 400 | Action/lý do/version input không hợp lệ |
| 401 | Chưa xác thực hoặc thiếu user identity hợp lệ |
| 403 | Role không có quyền ghi |
| 404 | Đơn không có hoặc ngoài phạm vi trước khi xử lý |
| 409 | Trạng thái/chu kỳ báo sai hoặc quyền đã thay đổi sau khi đọc |

Service đọc detail theo scope, kiểm tra version rồi mở transaction. Repository
dùng một UPDATE có điều kiện `NEEDS_REVIEW + ReportedAt + history ID + phân công
hiện tại + branch chưa xóa`. Chỉ một request thắng. Cập nhật này thực thi ngay,
không phải stage trong EF, nên bắt buộc nằm trong transaction của service.

Sau UPDATE, stage OrderAdjustment và SaveChanges rồi Commit. Nếu stage/save/commit
lỗi, rollback; không có chuyện giữ trạng thái mới nhưng thiếu history khi save lỗi.
Không thêm optimistic concurrency token vào generated EF models.

History ID chặn request của chu kỳ báo sai trước ngay cả khi SQL datetime2(0)
làm hai báo sai có cùng timestamp. Nếu không có history, latestHistoryId là 0.
Trạng thái và ngày xử lý lấy từ server; không tin các giá trị sau xử lý do Web gửi.

## Lịch sử và giao diện

- GET chi tiết ADMIN/OWNER có `history`, `latestHistoryId` và reporter ID.
- Mỗi history hiển thị trạng thái trước/sau, lý do, tên người xử lý và thời gian UTC.
  Không trả raw JSON BeforeData/AfterData ra Web.
- JSON history lưu mã/chi nhánh/người tạo/ngày/trạng thái/tổng tiền/report và snapshot
  các món, không lồng toàn bộ history cũ vào history mới.
- History cũ khác cấu trúc vẫn hiển thị lý do/người/thời gian, status có thể “Không rõ”.
- Form chỉ hiện cho ADMIN khi NEEDS_REVIEW; lý do bắt buộc; nút KEEP/CANCEL mở bước
  xác nhận riêng, có Quay lại. Hủy có cảnh báo không tính vào giá trị hoàn tất.
- Đang gửi khóa control. Không tự retry POST. Sau lỗi mạng/kết quả bất định hoặc
  409, yêu cầu tải lại đơn trước thao tác tiếp. Thành công tải lại detail/history.

Flow:

```text
OrderReviewPanel → OrderManagementApiService → AuthorizedApiService
→ OrderReviewController → OrderReviewService
→ OrderManagementService (scope/snapshot) + OrderRepository + UnitOfWork
→ SQL conditional UPDATE + OrderAdjustment INSERT → Commit
```

## Bằng chứng kiểm chứng

- 89/89 automated tests đạt (69 baseline + 20 test B4.2a).
- Release build toàn solution sạch 0 warning/error sau dừng host local giữ DLL.
- Unit test: role/user, input, scope, version cũ, KEEP/CANCEL, snapshot không đổi,
  một lần Save/Commit, UPDATE thua không history, rollback/clear tracking khi save lỗi,
  history mapping và HTTP client POST một lần không retry.
- SQL local integration bằng hai DbContext đồng thời: chỉ một thành công, một
  conflict, một history. KEEP/CANCEL đúng; lỗi history save rollback trạng thái;
  same-second old-cycle bị chặn; amount/report gốc không đổi.
- HTTP thật qua middleware/DI/SQL: anonymous 401; OWNER và EMPLOYEE 403; action
  sai/lý do trắng 400; đơn không có 404; KEEP/CANCEL 200; gửi lại 409;
  GET history có đúng một history/version cho đơn thử.
- Fixture dùng prefix ngẫu nhiên trong database localhost, không đụng bản ghi
  sẵn có; đã xóa fixture/history sau kiểm tra. Có thể để khoảng trống identity;
  không reseed, không gọi database Azure. Login thử chỉ phát sinh auth sessions.
- Probe tạm ở artifacts bị Git ignore, không chứa password/token. Không thay đổi
  hai tools DemoSeed/PasswordReset của người dùng.

## Cần nghiệm thu giao diện

1. ADMIN mở một đơn NEEDS_REVIEW: lý do trống bị chặn, KEEP/CANCEL có xác nhận,
   Quay lại không ghi. Xác nhận trên đơn dev thử, trạng thái/history đổi đúng.
2. OWNER không thấy form nhưng xem được history; EMPLOYEE không vào được quản lý.
3. Hai tab cùng detail: tab đầu xử lý thành công, tab sau 409 rồi tải lại.
4. Mất kết nối trong lúc gửi: không bấm gửi lại mù quáng; tải lại trạng thái/history.
5. Kiểm tra 390px/desktop: textarea, hai nút, bước xác nhận và history không tràn.

Không khẳng định đã test những ca browser trên chỉ từ Razor compile thành công.
B4.2b điều chỉnh món/giá và audit toàn hệ thống chưa triển khai.
