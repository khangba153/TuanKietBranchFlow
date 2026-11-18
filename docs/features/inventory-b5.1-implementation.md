# B5.1 — Dữ liệu nền kho và tồn theo chi nhánh

Ngày: 2026-11-18. Đã triển khai và kiểm chứng local; chưa commit/push/deploy.
PM duyệt tiến hành sau khi chốt hướng hoàn thiện project hiện tại; không mở CR-001.

## Chức năng đã có

- `/inventory`: xem tồn theo chi nhánh, phân trang 20 mục, tìm tên, lọc tồn và active.
- Bốn số tổng hợp: đã cấu hình, hết hàng, sắp hết, chưa cấu hình. Summary áp dụng
  tên/trạng thái nguyên liệu trước bộ lọc tồn và phân trang; không cộng các đơn vị khác nhau.
- Chưa có BranchIngredient thì Quantity/Threshold null, không giả thành 0 và GET không ghi.
- ADMIN được phân công cấu hình nguyên liệu đang hoạt động vào chi nhánh đang hoạt động,
  Quantity luôn 0. Không có endpoint sửa Quantity; trường dư Quantity không được ánh xạ.
- Sửa ngưỡng không âm, tối đa decimal(18,3), không làm tròn đầu vào sai precision.
- Cập nhật ngưỡng dùng expectedRowVersion 8 byte; cũ/xung đột trả 409, chỉ sửa threshold/time.
- `/inventory/catalog`: ADMIN tạo/sửa tên/bật-tắt danh mục toàn hệ thống; OWNER chỉ xem.
  EMPLOYEE không đọc danh mục chung, xem tên/đơn vị qua endpoint tồn thuộc chi nhánh.
- Không đổi đơn vị nguyên liệu khi đã cấu hình kho/có giao dịch/kiểm kho.
- Tên đơn vị đã dùng bị khóa hoàn toàn: hệ thống không thể tự biết đổi tên nào làm thay
  đổi ý nghĩa phép đo. Tạo đơn vị mới nếu cần. UI và API cùng thực thi giới hạn này.
- Không tắt đơn vị còn nguyên liệu hoạt động; bật lại nguyên liệu phải có đơn vị active.
- Nguyên liệu ngừng dùng vẫn hiện số tồn, không xóa lịch sử/cấu hình.
- OWNER xem chi nhánh chưa xóa gồm inactive; ADMIN/EMPLOYEE cần phân công còn hiệu lực.
- Hết hàng = 0; sắp hết = tồn dương <= ngưỡng dương; ngưỡng 0 không báo sắp hết.

## Flow và file chính

Inventory.razor / InventoryCatalog.razor -> InventoryApiService -> ba controller
Inventory/Ingredients/IngredientUnits -> InventoryService -> InventoryRepository
và BranchRepository -> UnitOfWork -> SQL local.

- DTO, service, repository, controller, Web service/page và test đều gom thư mục Inventory.
- Models và DbContext Database First giữ nguyên; local có decimal(18,3), unique và RowVersion.
- Một application lock SQL trong transaction điều phối các thao tác danh mục/cấu hình
  B5.1 để tránh tắt đơn vị đồng thời với tạo/bật nguyên liệu hoặc đổi đơn vị đồng thời
  với thêm cấu hình. Chỉ khóa ngắn khi ghi, không khóa toàn lượt xem tồn. Đây không phải
  thiết kế khóa cho nhập/xuất tương lai; B5.2 cần thiết kế riêng.
- UnitOfWork sở hữu Save/commit/rollback. Unique lỗi 2601/2627 chỉ trả conflict khi
  truy vấn lại xác nhận trùng, không che mọi DbUpdateException thành lỗi tên.
- RowVersion còn kiểm tra tại SQL khi lưu, không chỉ so sánh trên C#.

## Hợp đồng API

| Endpoint | Quyền | Thành công / lỗi dự kiến |
| --- | --- | --- |
| GET /api/ingredient-units?search= | ADMIN/OWNER | 200 danh sách InventoryUnitDTO; 400 từ khóa quá dài |
| POST /api/ingredient-units | ADMIN | body Name; 201 SavedId; 400 invalid; 409 trùng |
| PUT /api/ingredient-units/{id} | ADMIN | body Name; 200 SavedId; 404 missing; 409 trùng/đã dùng |
| PATCH /api/ingredient-units/{id}/status | ADMIN | body IsActive bool bắt buộc; 200; 409 còn nguyên liệu active |
| GET /api/ingredients?search= | ADMIN/OWNER | 200 danh sách InventoryIngredientDTO; 400 từ khóa quá dài |
| POST /api/ingredients | ADMIN | Name, UnitId; 201; 400 invalid; 409 trùng/đơn vị inactive |
| PUT /api/ingredients/{id} | ADMIN | Name, UnitId; 200; 404; 409 đơn vị đã dùng/trùng/inactive |
| PATCH /api/ingredients/{id}/status | ADMIN | IsActive bool bắt buộc; 200; 404; 409 đơn vị inactive |
| GET /api/inventory | cả ba role | branchId bắt buộc, search/isActive/stockStatus/page; 200 InventoryPageDTO; 400/403/404 |
| POST /api/inventory/configurations | ADMIN | BranchId,IngredientId,WarningThreshold; 201 SavedId; 400/403/404/409 |
| PUT /api/inventory/configurations/{id}/warning-threshold | ADMIN | WarningThreshold,ExpectedRowVersion base64; 200 SavedId; 400/403/404/409 |

Tất cả endpoint yêu cầu xác thực; sai role trả 403, không có phiên trả 401.
stockStatus: UNCONFIGURED/OUT/LOW/AVAILABLE hoặc bỏ trống; page từ 1 đến 10000.
Lỗi trả ProblemDetails, không trả entity/navigation/password/token.
Danh mục chung tải một danh sách (không phân trang) cho phạm vi đồ án hiện tại;
trang tồn mới phân trang ở SQL. Không gọi đây là tối ưu cho danh mục lớn thương mại.

## UI/UX

- Be Vietnam Pro và màu hiện có; không emoji/vector trang trí.
- Mobile dạng thẻ, desktop hàng grid; nút/input tối thiểu 44px.
- Summary đưa lên trước, trạng thái lọc trong details thu gọn.
- Form sửa tự focus; cảnh báo rời trang khi chưa lưu; block navigation khi đang ghi.
- Ngừng/bật danh mục xác nhận rõ phạm vi toàn hệ thống.
- Khi timeout/mất phản hồi/5xx/body thành công không đọc được: khóa ghi tiếp,
  yêu cầu tải lại đối chiếu; không retry POST/PUT/PATCH tự động.

## Bằng chứng kiểm chứng

- Debug build trước chỉnh UI cuối: 0 warning/error.
- Release build bản cuối: 0 warning/error; 219/219 test, gồm 28 case mới cho kho.
- SQL/HTTP probe local riêng: 25 kiểm tra đạt, fixture dọn trong finally. Probe lưu
  dưới artifacts/inventory-b51-probe (ignored, không đưa secrets/token vào output).
- Đã kiểm tra tạo unit/ingredient 201; tên trùng 409; concurrent cấu hình 201/409;
  concurrent threshold cùng version 200/409; gửi Quantity=999 vẫn lưu 0; precision
  vượt 3 số lẻ 400; used-unit/disable guard 409; inactive branch write 409.
- SQL phân trang 25 nguyên liệu -> 20+5 không trùng; summary trước status filter;
  nguyên liệu ngừng dùng còn tồn vẫn hiện; early validation rollback giải phóng lock.
- HTTP anonymous 401, OWNER write 403, EMPLOYEE global catalog 403, assigned stock 200,
  ADMIN cross-branch 403. Đây là API test, chưa thay cho browser từng role.
- Browser ADMIN local: đăng nhập, mở menu Kho/danh mục, tạo nguyên liệu, nhìn thấy
  Chưa cấu hình/null, chặn 4 số lẻ và lưu 1,234; sau lưu tồn 0 và Hết hàng đúng.
- Đã xem mobile 390px trước refinement; phát hiện filters dài và sửa. Sau restart
  Release Web phiên RAM hết hiệu lực đúng thiết kế; browser connector mất kết nối
  khi đăng nhập lại nên chưa xác nhận ảnh mobile/desktop cuối, focus/dirty-dialog,
  browser OWNER/EMPLOYEE và trường hợp mất mạng. Cần nghiệm thu các mục này tiếp.
- Nguyên liệu browser test và mọi fixture SQL đã dọn; không seed tồn giả vào dữ liệu demo.

## Phạm vi chưa làm

Chưa IN/OUT, reversal, kiểm kê, tự trừ tồn theo đơn, giá vốn, lô/hạn dùng, supplier,
BOM, QR/AI, hệ thống quản lý SOP hay chứng nhận ISO. B5.2 trở đi cần duyệt riêng.

Script 008 chuẩn bị quyền tối thiểu B5.1 cho branchflow_api trên DB demo, không có DELETE,
không cấp UPDATE Quantity. Chưa chạy script trên Azure; chưa kiểm chứng runtime bằng
cloud database user hoặc deploy. Nếu user hiện có quyền rộng từ trước, GRANT mới không
tự thu hồi quyền cũ; phải kiểm tra effective permissions khi chuẩn bị deploy.

## Nghiệm thu tiếp

### Kiểm tra bổ sung bằng trình duyệt tích hợp — 2026-11-18

- Đã khôi phục kiểm thử bằng IAB; Chrome extension/native vẫn không kết nối an toàn.
- ADMIN đăng nhập, mở kho và danh mục bản Release cuối; mobile 320px/390px và desktop
  1440px đã quan sát. Summary đứng trước filters, bộ lọc trạng thái thu gọn đúng.
- Không thấy tràn ngang sau khi viewport ổn định (DOM 390px scrollWidth 375px;
  desktop 1440px scrollWidth 1440px). Ảnh ngay khi đổi viewport có thể là frame cũ,
  đã chụp lại và đối chiếu DOM trước khi kết luận.
- Nhập tên nguyên liệu nhưng không lưu, bấm rời trang có confirm; dismiss giữ đúng
  URL và nội dung. Sau đó xóa nội dung kiểm thử, không tạo thêm dữ liệu database.
- EMPLOYEE employee01: chỉ có Chi nhánh số 2 trong dropdown, không có link danh mục;
  mở trực tiếp /inventory/catalog hiện không có quyền và không render form quản trị.
- OWNER: xem hai chi nhánh, xem đơn vị g/kg/lít/ml; không có form tạo/sửa hoặc nút đổi
  trạng thái. Ảnh minh chứng local: artifacts/inventory-b51-probe/owner-catalog.jpg.
- Đã reset viewport sau kiểm tra. Không sửa code hoặc database trong lượt UI này.
- Còn chưa kiểm: màn hàng tồn có dữ liệu trên bố cục cuối, breakpoint 768px,
  focus form sửa ngưỡng và thử mất phản hồi khi ghi. Không đánh dấu toàn bộ UI đạt.

1. ADMIN: /inventory và /inventory/catalog desktop/mobile 320/390/768/1440px.
2. Kiểm tra form sửa tự focus, bỏ thay đổi có xác nhận, không mất nội dung vì đổi tab.
3. OWNER chỉ xem; EMPLOYEE chỉ kho đúng chi nhánh, không hiện quản trị danh mục.
4. Mất phản hồi khi lưu: không gửi lại, tải lại đối chiếu thực tế.
5. Chỉ commit/push/deploy khi PM yêu cầu riêng.
