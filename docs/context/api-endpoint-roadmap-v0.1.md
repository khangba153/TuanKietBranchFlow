# BranchFlow API endpoint roadmap v0.1

## Cơ sở thiết kế

Roadmap này được đối chiếu từ:

- nghiệp vụ trong `business-requirements-v1.5.md`;
- 30 bảng của `BranchFlowDB`;
- 71 ảnh trong `PrototypeUI`: ADMIN 32, EMPLOYEE 19 và OWNER 20;
- ba vai trò `OWNER`, `ADMIN`, `EMPLOYEE` và phạm vi chi nhánh.

Đây là kế hoạch ban đầu, chưa phải hợp đồng API đã khóa. Khi triển khai từng
chức năng vẫn phải chốt request DTO, response DTO, status code và phân quyền.

## Nguyên tắc từ prototype

- Một màn hình có thể gọi nhiều endpoint.
- Nhiều màn hình có thể dùng chung một endpoint với bộ lọc và quyền khác nhau.
- Popup, tab, phân trang và trạng thái chọn trên form không tự tạo thành endpoint.
- Giỏ món của employee được giữ ở state phía Web. MVP không lưu draft nên chỉ
  gọi API khi người dùng xác nhận tạo order.
- OWNER dùng lại endpoint đọc của ADMIN nhưng API bắt buộc giữ OWNER read-only.
- Nút đổi vai trò trong prototype chỉ để trình diễn, không phải chức năng production.

## Giai đoạn 1 - Đăng nhập và phạm vi người dùng

Mục tiêu: biết người gọi là ai, có role gì và được truy cập chi nhánh nào.

| Method | Route | Mục đích |
|---|---|---|
| POST | `/api/auth/login` | Đăng nhập và nhận JWT |
| GET | `/api/auth/me` | Đọc identity, role và branch scope từ JWT |
| GET | `/api/users/me` | Lấy hồ sơ cá nhân |
| PUT | `/api/users/me` | Cập nhật hồ sơ cá nhân được phép |
| PUT | `/api/users/me/password` | Đổi mật khẩu |
| GET | `/api/branches/accessible` | Lấy chi nhánh người dùng được phép xem |
| GET | `/api/branches/{branchId}` | Lấy thông tin một chi nhánh trong scope |
| GET | `/api/roles` | Hiển thị ba role cố định |

Tổng dự kiến: **8 endpoint**.

## Giai đoạn 2 - Menu nền và màn hình gọi món

Mục tiêu: employee xem được menu theo chi nhánh; admin quản lý dữ liệu menu dùng
chung và trạng thái bán riêng của chi nhánh.

### Menu gọi món

| Method | Route | Mục đích |
|---|---|---|
| GET | `/api/order-menu?branchId={id}` | Trả category, product, size, topping và note đang bán |

### Category

| Method | Route |
|---|---|
| GET | `/api/categories` |
| POST | `/api/categories` |
| PUT | `/api/categories/{id}` |
| PATCH | `/api/categories/{id}/status` |

### Size

| Method | Route |
|---|---|
| GET | `/api/sizes` |
| POST | `/api/sizes` |
| PUT | `/api/sizes/{id}` |
| PATCH | `/api/sizes/{id}/status` |

### Product và giá theo size

| Method | Route |
|---|---|
| GET | `/api/products` |
| GET | `/api/products/{id}` |
| POST | `/api/products` |
| PUT | `/api/products/{id}` |
| PATCH | `/api/products/{id}/status` |
| PATCH | `/api/products/{id}/branches/{branchId}/availability` |

`POST` và `PUT Product` nhận danh sách size/giá trong cùng request để bảo đảm
quy tắc một product có ít nhất hai size hoạt động.

### Topping group và topping

| Method | Route |
|---|---|
| GET | `/api/topping-groups` |
| POST | `/api/topping-groups` |
| PUT | `/api/topping-groups/{id}` |
| PATCH | `/api/topping-groups/{id}/status` |
| PATCH | `/api/toppings/{id}/branches/{branchId}/availability` |

### Note group và note option

| Method | Route |
|---|---|
| GET | `/api/note-groups` |
| POST | `/api/note-groups` |
| PUT | `/api/note-groups/{id}` |
| PATCH | `/api/note-groups/{id}/status` |

Tổng dự kiến: **24 endpoint**.

## Giai đoạn 3 - Order

Mục tiêu: hoàn thành luồng gọi món chính trước, sau đó mới làm điều chỉnh của admin.

| Method | Route | Mục đích |
|---|---|---|
| POST | `/api/orders` | Tạo order và toàn bộ item/topping/note trong transaction |
| GET | `/api/orders` | Danh sách order theo branch, ngày, trạng thái, employee |
| GET | `/api/orders/mine` | Employee xem order do mình tạo |
| GET | `/api/orders/{id}` | Xem snapshot chi tiết order |
| POST | `/api/orders/{id}/report-error` | Employee báo sai order |
| POST | `/api/orders/{id}/adjustments` | Admin điều chỉnh và lưu before/after |
| POST | `/api/orders/{id}/cancel` | Hủy order theo nghiệp vụ được phép |

Tổng dự kiến: **7 endpoint**.

## Giai đoạn 4 - Kho và kiểm kho

### Tồn kho

| Method | Route |
|---|---|
| GET | `/api/inventory?branchId={id}` |

### Ingredient

| Method | Route |
|---|---|
| GET | `/api/ingredients` |
| POST | `/api/ingredients` |
| PUT | `/api/ingredients/{id}` |
| PATCH | `/api/ingredients/{id}/status` |

### Ingredient unit

| Method | Route |
|---|---|
| GET | `/api/ingredient-units` |
| POST | `/api/ingredient-units` |
| PUT | `/api/ingredient-units/{id}` |
| PATCH | `/api/ingredient-units/{id}/status` |

### Giao dịch kho

| Method | Route | Mục đích |
|---|---|---|
| GET | `/api/stock-transactions` | Danh sách giao dịch |
| GET | `/api/stock-transactions/{id}` | Chi tiết nhiều dòng nguyên liệu |
| POST | `/api/stock-transactions` | Tạo giao dịch IN hoặc OUT |
| POST | `/api/stock-transactions/{id}/reverse` | Hoàn tác bằng giao dịch REVERSAL mới |

### Kiểm kho

| Method | Route | Mục đích |
|---|---|---|
| GET | `/api/stocktakes` | Lịch sử kiểm kho |
| GET | `/api/stocktakes/{id}` | Chi tiết kiểm kho và chênh lệch |
| POST | `/api/stocktakes` | Hoàn tất kiểm kho và tạo điều chỉnh nếu có lệch |

Tổng dự kiến: **16 endpoint**.

## Giai đoạn 5 - Cấu hình, nhân viên và bảng lương

### Cấu hình chi nhánh

| Method | Route |
|---|---|
| GET | `/api/branches` |
| POST | `/api/branches` |
| PUT | `/api/branches/{id}` |
| PATCH | `/api/branches/{id}/status` |

### Tài khoản

| Method | Route |
|---|---|
| GET | `/api/users` |
| POST | `/api/users` |
| PUT | `/api/users/{id}` |
| PATCH | `/api/users/{id}/status` |
| POST | `/api/users/{id}/reset-password` |

### Nhân viên

| Method | Route |
|---|---|
| GET | `/api/employees` |
| GET | `/api/employees/{id}` |
| POST | `/api/employees` |
| PUT | `/api/employees/{id}` |
| PUT | `/api/employees/{id}/current-branch` |
| PATCH | `/api/employees/{id}/status` |

Tạo employee có thể đồng thời tạo `AppUser`, `EmployeeProfile` và `UserBranch`
trong một transaction nếu form yêu cầu tạo tài khoản cùng lúc.

### Payroll

| Method | Route |
|---|---|
| GET | `/api/payrolls` |
| GET | `/api/payrolls/{id}` |
| GET | `/api/payrolls/mine` |
| POST | `/api/payrolls` |
| PUT | `/api/payrolls/{id}` |
| PATCH | `/api/payrolls/{id}/paid` |

Tổng dự kiến: **21 endpoint**.

## Giai đoạn 6 - Dashboard, báo cáo và audit

| Method | Route | Mục đích |
|---|---|---|
| GET | `/api/dashboard/employee` | Tổng đơn và doanh thu cá nhân hôm nay |
| GET | `/api/dashboard/management` | KPI theo role và branch scope |
| GET | `/api/reports/operations` | Doanh thu, món bán, kho và tổng lương theo bộ lọc |
| GET | `/api/audit-logs` | Danh sách audit có lọc và phân trang |
| GET | `/api/audit-logs/{id}` | Xem JSON trước/sau của một audit |

Tổng dự kiến: **5 endpoint**.

## Tổng hợp và thứ tự triển khai

| Thứ tự | Nhóm | Endpoint dự kiến |
|---:|---|---:|
| 1 | Đăng nhập và branch scope | 8 |
| 2 | Menu và dữ liệu gọi món | 24 |
| 3 | Order | 7 |
| 4 | Kho và kiểm kho | 16 |
| 5 | Cấu hình, nhân viên, payroll | 21 |
| 6 | Dashboard, báo cáo, audit | 5 |
| | **Tổng dự kiến** | **81** |

Thứ tự này ưu tiên một vertical slice có thể demo được: đăng nhập -> tải menu ->
tạo order -> xem lại order. Các màn hình quản trị và báo cáo dùng dữ liệu thật được
phát triển sau khi flow chính đã ổn định.

## Thành phần UI không tạo endpoint riêng trong MVP

- Thêm, sửa, xóa món trong giỏ trước khi xác nhận: state của Blazor.
- Popup chọn size, topping, note: dùng dữ liệu từ `/api/order-menu`.
- Popup xác nhận tạo order: chỉ gọi `POST /api/orders` khi xác nhận.
- Tab, bộ lọc, đóng/mở drawer và toast: logic UI.
- Công tắc chuyển role trong prototype: chỉ là chế độ xem demo.
- Chuông thông báo: chưa có bảng và nghiệp vụ notification trong MVP.
- Export báo cáo: ngoài MVP hiện tại.
