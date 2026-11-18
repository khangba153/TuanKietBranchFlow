# B5.1 — Khảo sát và đề xuất dữ liệu nền kho

Ngày khảo sát: 2026-11-18. Trạng thái: ĐÃ DUYỆT, ĐÃ TRIỂN KHAI LOCAL; CHỜ NGHIỆM THU UI CUỐI.
Nội dung khảo sát/đề xuất bên dưới được giữ để đối chiếu phạm vi ban đầu.
Kết quả triển khai, hợp đồng API và giới hạn kiểm chứng: [inventory-b5.1-implementation.md](inventory-b5.1-implementation.md).
Đã đối chiếu ba bảng nền trên SQL local, không đổi schema; chưa truy cập/cấp quyền/deploy Azure.

## Bằng chứng hiện có

- Bảy EF model và mapping đã có: IngredientUnit, Ingredient, BranchIngredient,
  StockTransaction, StockTransactionDetail, Stocktake, StocktakeItem.
- Schema đối chiếu: database/azure/001-initial-schema.sql, nhóm kho từ IngredientUnit
  đến StocktakeItem. Đây là schema nguồn trong repo, không khẳng định database Azure
  hiện tại đang khớp hoàn toàn.
- Chưa tìm thấy kho controller/service/DTO/repository feature trong các tầng ứng dụng;
  các menu Kho ở Web vẫn disabled/Sắp có.
- BranchIngredient có unique (BranchId,IngredientId), Quantity/WarningThreshold
  decimal(18,3), CHECK không âm và RowVersion được EF map concurrency token.
- Ingredient.Name và IngredientUnit.Name unique khi Deleted=0; bảng nguyên liệu có
  đơn vị, active/deleted nhưng không có mã nguyên liệu, công thức pha chế hoặc đơn vị quy đổi.
- StockTransactionDetail giữ before/change/after; ledger không soft delete.
- Stocktake là phiếu đã hoàn tất, không có draft/status trong schema hiện tại.
- Lịch sử giao dịch hiện tham chiếu tên/đơn vị hiện tại, chưa có snapshot tên/đơn vị.
  Không được hứa đổi đơn vị mà lịch sử vẫn giữ đúng ý nghĩa cũ.

## Baseline nghiệp vụ giữ lại

Theo business-requirements-v1.5 và roadmap v0.2/v0.3:

- Nguyên liệu/đơn vị dùng chung; tồn/ngưỡng cảnh báo theo chi nhánh; không âm kho.
- Chỉ trừ tồn khi ghi nhận xuất kho; bán/tạo/hoàn tất đơn hàng không trừ nguyên liệu.
  Đây là hướng nghiệp vụ đã chốt, không phải tính năng chờ recipe/BOM để làm sau.
- ADMIN nhập kho, EMPLOYEE xuất kho theo phân công; OWNER chỉ xem.
- Giao dịch sai dùng reversal, không sửa/xóa bản ghi đã ghi nhận.
- Kiểm kho hoàn tất một lần, có lệch sinh COUNT_ADJUSTMENT; kiểm một phần không
  được biến các nguyên liệu không kiểm thành tồn 0.
- Chưa làm nhà cung cấp, giá nhập/giá vốn, giá trị tồn, lô/hạn dùng hoặc quy đổi đơn vị.
- Tài liệu cũ có giả định chỉ một ADMIN; giả định đó không được ghi đè workflow
  nhiều ADMIN đã chốt và triển khai trong mốc điều chuyển nhân viên.

## B5.1 đề xuất — đi hết Web/API/database/test

### Danh mục dùng chung

- Danh sách/tìm kiếm/tạo/sửa tên/bật-tắt đơn vị tính và nguyên liệu.
- Đơn vị tối đa 50 ký tự, nguyên liệu tối đa 150; trim tên, cấm trống, lỗi unique 409.
- Chọn một đơn vị đang hoạt động cho nguyên liệu mới. Chưa nhập quy đổi kg/g/ml.
- ADMIN được sửa danh mục chung giống menu hiện tại; UI phải ghi rõ áp dụng toàn hệ thống.
- OWNER chỉ đọc; EMPLOYEE chỉ đọc dữ liệu nguyên liệu/đơn vị cần cho tồn kho thuộc chi nhánh,
  không có quyền sửa danh mục.
- Mốc đầu chỉ bật/tắt, chưa thêm xóa/restore. Không hard delete hoặc xóa lịch sử.
- Đề xuất không đổi đơn vị sau khi nguyên liệu đã được cấu hình ở bất kỳ kho chi nhánh
  hoặc đã có giao dịch/kiểm kho; tránh đổi cách hiểu số tồn. Tên đơn vị đã được dùng
  không đổi thành một loại đo khác; nếu cần loại đo mới thì tạo đơn vị mới.
- Không tắt đơn vị khi còn nguyên liệu hoạt động sử dụng. Nguyên liệu dừng dùng còn tồn
  vẫn hiển thị tồn, không làm mất dấu số lượng. Luật ghi giao dịch ra khỏi nguyên liệu
  dừng dùng cần chốt chi tiết ở B5.2, không tự khóa khiến tồn không xử lý được.

### Cấu hình nguyên liệu tại chi nhánh

- ADMIN được phân công thêm nguyên liệu vào kho chi nhánh đang hoạt động và đặt ngưỡng cảnh báo.
- Bản ghi mới bắt đầu Quantity=0, không cho nhập số tồn ban đầu trực tiếp trong form này.
- Không tự nhân bản mọi nguyên liệu sang mọi chi nhánh; không tạo hàng kho trong GET.
- Một nguyên liệu chưa có BranchIngredient hiển thị là Chưa cấu hình, không giả thành tồn 0.
- Không tháo/xóa cấu hình đã dùng; không có endpoint sửa Quantity trong B5.1.
- Sau này số tồn ban đầu được ghi bằng giao dịch IN, có người/thời gian và lịch sử.
- Chi nhánh ngừng hoạt động chỉ xem trong B5.1, không thêm cấu hình hoặc sửa ngưỡng.
- Ngưỡng không âm, tối đa 3 chữ số thập phân; không tự làm tròn dữ liệu vượt precision.
- Dùng RowVersion cho cập nhật ngưỡng; conflict 409 yêu cầu tải lại, không ghi đè mù quáng.

### Xem tồn và UX

- OWNER chọn mọi chi nhánh chưa xóa, gồm ngừng hoạt động để đọc; ADMIN/EMPLOYEE chỉ
  xem chi nhánh có phân công hiện tại. API kiểm tra scope, không dựa vào dropdown.
- Phân trang, tìm theo tên, lọc trạng thái tồn và trạng thái nguyên liệu.
- Thẻ tổng hợp số loại đã cấu hình, hết hàng, sắp hết và chưa cấu hình.
- Không cộng kg+ml+cái thành một tổng số lượng vô nghĩa; không gọi số lượng là giá trị tồn.
- Đề xuất Hết hàng: Quantity=0; Sắp hết: Quantity>0 và Quantity<=WarningThreshold
  khi WarningThreshold>0. Ngưỡng 0 là không bật cảnh báo sắp hết, vẫn báo hết hàng.
- Hiển thị số lượng kèm đơn vị và dấu thời gian cập nhật; tên dài xuống dòng được.
- Desktop dạng bảng gọn; mobile dạng thẻ hoặc hàng responsive, hành động dễ chạm.
- Be Vietnam Pro, giữ nhận diện hiện tại, không emoji/vector trang trí.
- Loading/empty/forbidden/error/unknown-result có hướng xử lý, không để form tự retry ghi.

## API dự kiến, chưa phải hợp đồng đã triển khai

- /api/ingredient-units: GET, POST, PUT /{id}, PATCH /{id}/status.
- /api/ingredients: GET, POST, PUT /{id}, PATCH /{id}/status.
- GET /api/inventory?branchId=... với bộ lọc/phân trang/summary.
- POST /api/inventory/configurations: branchId, ingredientId, warningThreshold; không Quantity.
- PUT /api/inventory/configurations/{id}/warning-threshold: threshold + expectedRowVersion.
- Repository trong Infrastructure/Repositories/Inventory, service/DTO theo cùng nhóm;
  trang và CSS cạnh nhau trong Web/Components/Pages/Inventory.
- Controller mỏng, DTO không trả graph EF; scope theo branch ở service và query SQL.
- Dự kiến tận dụng schema hiện có, chưa thấy cần thay schema cho B5.1. Trước khi code
  phải kiểm tra database local thực tế và quyền database user; script cấp quyền cloud
  nếu cần chỉ chuẩn bị và chờ duyệt chạy, không tự truy cập Azure.

## Thứ tự sau B5.1

1. B5.2 nhập/xuất nhiều dòng và lịch sử; chống âm, đồng thời, idempotency/chống gửi lặp.
2. B5.3 reversal đúng một lần, đúng chi nhánh, không âm khi đảo giao dịch IN; lý do/audit.
3. B5.4 kiểm kho một phần, xử lý thay đổi tồn lúc đang đếm, hoàn tất một lần và adjustment.

Mỗi mốc có đề xuất quy tắc và duyệt riêng; không coi duyệt B5.1 là duyệt mọi chi tiết sau.

## Tiêu chí nghiệm thu B5.1

- Tạo nguyên liệu/đơn vị trùng, sai role/scope, số âm/vượt 3 số lẻ đều bị từ chối đúng lỗi.
- Cấu hình mới Quantity=0; gửi Quantity từ client không làm thay đổi tồn; GET không tạo bản ghi.
- Hai ADMIN thêm cùng nguyên liệu/chi nhánh chỉ có một bản ghi; update ngưỡng stale trả conflict.
- Không đổi đơn vị đã dùng, không ẩn mất nguyên liệu dừng dùng còn tồn.
- Summary đúng theo bộ lọc được mô tả, không cộng các đơn vị khác loại.
- HTTP role/scope + SQL local + browser desktop/mobile, không chỉ build/test unit.
- Chưa có dữ liệu thì hiển thị đúng rỗng/chưa cấu hình, không seed giả để tuyên bố đã có tồn.

## Các quyết định đã duyệt cùng phạm vi

1. ADMIN được sửa danh mục nguyên liệu/đơn vị dùng chung như menu, chưa có admin tổng.
2. Cấu hình kho theo từng chi nhánh, số tồn ban đầu=0, chưa có màn sửa số tồn trực tiếp.
3. Không đổi đơn vị của nguyên liệu đã cấu hình kho/đã có lịch sử; mốc đầu bật/tắt, không xóa.
4. Cảnh báo sắp hết khi tồn dương <= ngưỡng; ngưỡng 0 tắt sắp hết nhưng vẫn báo hết hàng.

Không thay đổi chức năng trong lượt khảo sát này. Phần nhân viên hardening đã push
ef37906; còn checklist nghiệm thu tương tác form tạo/mất phản hồi/mobile, không suy ra
đã đạt chỉ từ yêu cầu commit/push.
