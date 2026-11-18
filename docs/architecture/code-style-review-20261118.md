# Rà phong cách source — 2026-11-18

## Chuẩn lưu lâu dài

- `AGENTS.md`: quy định mặc định cho các lượt làm việc sau, dẫn tới checklist chi tiết.
- `.cursor/rules/branchflow-code-style.mdc`: checklist luôn áp dụng cho code viết tay.
- `.editorconfig`: hỗ trợ editor/formatter; không thay thế việc đọc flow và rà diff.
- Mẫu gốc: Employee tại Git `f07bd06`, Order tại `16172d5`. Chỉ tham khảo phong cách,
  không phục hồi auth cũ hay sao chép lỗi bảo mật/concurrency của code lịch sử.

## Phạm vi và thay đổi

Đã quét 356 file viết tay: API 22, Application 163, Infrastructure 47, Tests 20,
Web 104. Đây là rà cấu trúc/pattern toàn phạm vi kết hợp đọc các luồng tiêu biểu,
không phải tuyên bố đã kiểm chứng thủ công từng dòng hoặc mọi nhánh nghiệp vụ.

- Ưu tiên khai báo kiểu rõ, `new Type()`, tên dependency thể hiện trách nhiệm.
- Tách câu lệnh/thuộc tính initializer, thêm braces, xuống dòng tham số dài.
- Ánh xạ DTO rõ bước; không nén nhiều thao tác UI hoặc nhiều thẻ Razor một dòng.
- Chuẩn hóa tên state UI dễ hiểu và bố cục markup/code-behind.
- Catalog kho tách sáu method: CreateUnitAsync, UpdateUnitAsync,
  UpdateUnitStatusAsync, CreateIngredientAsync, UpdateIngredientAsync,
  UpdateIngredientStatusAsync. Controller gọi đúng use case; nhánh HTTP rõ ràng.
- Giữ API route/DTO, transaction/SQL lock, kiểm tra quyền và quy tắc kho hiện có.

Không sửa generated Models/DbContext, schema, kiến trúc bốn project hoặc dependency
production. Không triển khai nhập/xuất/kiểm kê và không trừ nguyên liệu khi bán món.
Các công cụ audit/probe và ảnh kiểm chứng ở `artifacts/` là local, không thêm vào solution.

## Kiểm chứng

- Release build: 0 warning, 0 error.
- Unit tests: 219 đạt, 0 lỗi.
- Inventory SQL/HTTP probe: 25 kiểm tra đạt; bao gồm quyền, validation, race/conflict,
  rollback, phân trang, inactive và Quantity ban đầu bằng 0. Fixture được dọn sau probe.
- Browser smoke ADMIN trên host kiểm thử riêng: đăng nhập, inventory/catalog navigation,
  danh sách đơn, danh sách nhân viên và form sửa hồ sơ. Không lưu thay đổi form.
- Mobile 390x844: inventory không tràn ngang, mở/hủy sửa ngưỡng và focus input hoạt động.
- Desktop 1440x900: inventory/đơn/hồ sơ hiển thị; console không có error trong lượt kiểm tra.
- Ảnh local: `artifacts/style-audit/inventory-mobile.jpg` và `inventory-desktop.jpg`.
- `git diff --check` sạch; generated Models/DbContext không có diff.

Probe employee closeout đã được khởi chạy nhưng kết quả cuối không thu lại được trong
lượt công cụ, nên không tính là bằng chứng regression đã đạt của đợt này.
Không tuyên bố đã tương tác mọi trang hoặc kiểm lại toàn bộ role qua browser.

## Trạng thái bàn giao

Chưa commit/push/deploy. Bản API/Web chạy sẵn ở 5007/5032 không bị dừng để thay code.
Hai host kiểm thử riêng 5107/5132 đã dừng sau smoke test. Bộ seed lịch sử vẫn giữ nguyên;
không nhập các số lượng kiểm kho cũ thành tồn hiện tại trong đợt rà phong cách.
