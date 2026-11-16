# B4.2b — điều chỉnh đơn báo sai

Ngày triển khai local: 2026-11-16. Chưa commit/push/deploy. Phạm vi được duyệt:
sửa số lượng, bỏ dòng, đổi size cùng món, topping và ghi chú; không thêm món mới.
Đổi size dùng giá hiện tại do Backend lấy; giữ size dùng giá snapshot cũ.

## Quy tắc và giá

- Chỉ ADMIN có phân công hiện hành tại branch chưa xóa, chỉ đơn NEEDS_REVIEW.
  OWNER chỉ xem kết quả/lịch sử; EMPLOYEE giữ flow tạo/xem/báo sai riêng.
- Chỉ sửa các OrderItemId thuộc đơn hiện tại, không đổi sang sản phẩm khác.
- Đổi ProductSizeId: size mới phải đang hoạt động và sản phẩm được bán tại chi nhánh;
  dùng giá/name size hiện tại. Giữ size cũ: giữ tên size và giá snapshot dù giá menu đã đổi.
- Tên sản phẩm gốc vẫn giữ snapshot, không bị thay bởi tên menu mới.
- Topping đã có trên dòng: giữ tên/giá snapshot, kể cả đổi số lượng hoặc chọn lại;
  topping thêm mới: giá/tên hiện tại. Mọi topping được chọn phải còn hợp lệ tại branch.
  Topping thường tối đa 3, nhóm Up size tối đa 1, không trùng ID trong một dòng.
- Ghi chú được giữ phải đang hoạt động; một lựa chọn mỗi nhóm, không trùng.
  Giữ lựa chọn cũ thì giữ tên snapshot, lựa chọn mới dùng tên hiện tại.
- Không còn hợp lệ thì báo lỗi và yêu cầu bỏ/chọn lại, không âm thầm bỏ lựa chọn.
- Số lượng món 1–999, tối đa 100 dòng được giữ; không được bỏ toàn bộ đơn.
  Muốn bỏ toàn bộ dùng Hủy đơn. Lý do bắt buộc trim 1–500 ký tự.
- Tổng dòng = (giá size + tổng giá topping × số phần topping) × số món.
  Backend tự tính, request không có giá hay tổng tiền. Giới hạn decimal(18,2).
- Không cho xác nhận nếu không có thay đổi nội dung; dùng Giữ nguyên đơn thay thế.
- Sau lưu: ADJUSTED, tổng tiền/snapshot hiện hành đổi theo bản đã xác nhận;
  mã đơn, người tạo, branch, ngày nghiệp vụ và thông tin báo sai không đổi.
  Giá trị đơn vẫn thuộc ngày nghiệp vụ gốc, chưa phải tiền thực thu/hoàn tiền.

## API và flow

Tất cả route dưới `/api/order-management/{orderId}/adjustment`, role ADMIN:

| Method / suffix | Mục đích |
|---|---|
| GET `options` | Size hiện tại của các sản phẩm đã có trong đơn, topping và ghi chú hợp lệ |
| POST `preview` | Kiểm tra quyền/version/input, tính lại trước/sau; không ghi SQL |
| POST route gốc | Tính lại toàn bộ và so với bản xem trước, transaction lưu ADJUSTED/món/history |

Request có Reason, ExpectedReportedAt, ExpectedHistoryId và Items:
OrderItemId, ProductSizeId, Quantity, Toppings (ToppingId/Quantity), NoteOptionIds.
Dòng không gửi trong Items được bỏ khỏi bản hiện hành. Không nhận giá/tên từ client.
Khi xác nhận, thêm PreviewHash do preview trả về.

PreviewHash là SHA-256 của dữ liệu trước/sau, user, history version và lý do đã
chuẩn hóa. Hash không phải token xác thực, không thay JWT hoặc scope. Commit
tính lại từ SQL/menu; khác hash do giá/lựa chọn/request thay đổi trả 409, yêu cầu
tải lại/xem trước lại. Giá hiện tại là giá Backend đọc lại tại lần xác nhận, không
có cơ chế giữ giá/khóa giá menu dài hạn. Nếu giá đổi sau lần đọc đó, hệ thống không
cam kết một snapshot transaction chung với quản trị menu.

Status HTTP: 401 chưa xác thực, 403 role sai, 404 đơn không có/ngoài quyền,
400 dữ liệu sai hoặc không có thay đổi, 409 phiên báo sai/quyền/giá đã đổi,
200 options/preview/lưu thành công.

Flow:

```text
OrderAdjustmentPanel → OrderManagementApiService → OrderAdjustmentController
→ OrderAdjustmentService → OrderManagementService + OrderMenuRepository
→ OrderRepository.TryResolveReviewAsync (ADJUSTED, kiểm tra lại version/scope)
→ ApplyAdjustedItemsAsync + AddReviewHistoryAsync → UnitOfWork Save/Commit
```

## Tính nguyên tử và lịch sử

- Dùng conditional UPDATE đã có ở B4.2a để giành quyền xử lý trong transaction.
  Hai ADMIN cùng xử lý: một thắng, request còn lại 409, không ghi history trùng.
- Trước khi Save, repository tải tracked graph của đúng orderId đã giành quyền,
  giữ ID dòng còn lại; cập nhật size/qty/giá; thay topping/ghi chú, xóa dòng bỏ đi
  cùng child của dòng đó. Không xóa SalesOrder.
- Bản trước được lưu đầy đủ trong OrderAdjustment.BeforeData; AfterData là bản mới.
  Không lồng history cũ vào snapshot mới. Có lý do, actor ID, thời gian UTC.
- Marker ADJUSTED, dòng món, children, tổng tiền và history cùng transaction.
  Save lỗi rollback toàn bộ. Không đổi schema/generated models, không thêm package.
- GET detail mở rộng history có BeforeItems/AfterItems và BeforeTotalAmount/
  AfterTotalAmount. UI có mục mở rộng xem nội dung trước/sau; không trả raw JSON.
- Parser history vẫn chấp nhận history B4.2a cũ thiếu các ID mới. JSON cũ không
  đúng shape có thể không hiển thị chi tiết, nhưng lý do/người/thời gian vẫn còn.

## Giao diện

- ADMIN chọn Điều chỉnh đơn trong panel báo sai. Có lựa chọn size, số lượng,
  bỏ/giữ lại dòng, thêm/bỏ/số lượng topping và dropdown ghi chú theo nhóm.
- Trạng thái không còn hợp lệ được ghi rõ; Quay lại không lưu.
- Nhập lý do → Xem trước → so sánh từng dòng trước/sau và tổng tiền → Xác nhận.
  Bản xem trước đóng băng request, Sửa lại phải xem trước lại.
- Đang gửi khóa control; không tự retry thao tác ghi. Lỗi mạng/kết quả không rõ
  yêu cầu tải lại trước khi thử; 409 yêu cầu tải lại và tính preview mới.
- Sau lưu, tải lại chi tiết/history; OWNER không thấy form dù đơn NEEDS_REVIEW.

## Bằng chứng kiểm chứng

- Release build toàn solution: 0 warning/error.
- 112/112 unit test: 89 baseline + 19 test service điều chỉnh + 4 test Web client.
  Gồm giá cũ/mới, topping/ghi chú, foreign line/size, giới hạn/duplicate/no-op,
  version cũ, thay giá sau preview, commit một lần, race loser không ghi,
  rollback và HTTP client không retry.
- SQL integration trên database localhost (probe từ chối non-local): preview
  không ghi, giá snapshot/size mới đúng, thay children/bỏ dòng thực sự lưu được,
  history đầy đủ, gửi lại bị chặn; save lỗi trả về nguyên trạng; hai DbContext
  đồng thời một thắng, một conflict và đúng một history.
- HTTP thật qua middleware/DI/SQL: anonymous 401; OWNER/EMPLOYEE 403 cho options/ghi;
  invalid 400; options/preview/commit 200; hash sai/gửi lại 409; GET history trước/sau đúng.
- Browser Chrome local ADMIN ở 390px: đổi size 700ML giá 20.000đ, qty 2,
  thêm topping 5.000đ, giữ topping cũ 2.000đ, đổi ghi chú, bỏ dòng thứ hai;
  preview 46.000đ → 54.000đ và xác nhận thành ADJUSTED. Detail có một dòng,
  2 món, 54.000đ và history gốc/hậu điều chỉnh đúng.
- Quan sát history trước/sau ở 1440px, DOM đo pageWidth 1425 ≤ viewport 1440.
  OWNER không thấy form ở đơn NEEDS_REVIEW và xem được history đơn ADJUSTED.
- Dùng fixture prefix ngẫu nhiên; không sửa đơn sẵn có. Đã dọn toàn bộ fixture
  orders/items/toppings/notes/history; có thể để khoảng trống identity, không reseed.
  Database container local được khởi động lại do đang tắt; không gọi Azure.
- Probe nằm trong artifacts bị Git ignore, không có mật khẩu/token trong source;
  không thay hai tools seed/reset của người dùng.

## Giới hạn / nghiệm thu còn lại

- Chưa kiểm tra điện thoại vật lý, mọi breakpoint hoặc toàn bộ lỗi mạng qua UI.
- Chưa kiểm tra bằng browser khi giá thực sự đổi giữa hai bước (unit test hash đã đạt).
- Quyền SQL runtime cloud cần có UPDATE/INSERT/DELETE cho child hiện hành và INSERT
  OrderAdjustment theo cách lưu này; chưa thay grant hoặc deploy cloud.
- Chưa thêm sản phẩm mới, đổi món, nhập giá tùy ý, mở lại CANCELLED/ADJUSTED hoặc
  xử lý thanh toán/hoàn tiền. Không xem B4.2b là quy trình tài chính production đầy đủ.
