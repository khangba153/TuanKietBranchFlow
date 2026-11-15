# Tổ chức thư mục theo chức năng

Giữ nguyên bốn project ứng dụng và project test. Trong mỗi tầng, file được gom
theo nhóm nghiệp vụ; không tạo thêm project hoặc đổi kiến trúc runtime.

## Các nhóm chính

| Nhóm | Nội dung |
| --- | --- |
| `Auth` | Đăng nhập, JWT, phiên và token; Web có cookie và kho phiên |
| `Branches` | Chi nhánh và truy vấn phân công `UserBranch` |
| `Employees` | Hồ sơ và quản lý nhân viên |
| `Orders` | Menu gọi món, tạo/xem đơn và báo sai |
| `MenuManagement/Categories` | Danh mục sản phẩm |
| `MenuManagement/Products` | Sản phẩm và cấu hình size/giá |
| `MenuManagement/Sizes` | Danh mục size |
| `MenuManagement/Toppings` | Topping và nhóm topping |
| `MenuManagement/Notes` | Nhóm/lựa chọn ghi chú |
| `Users`, `Roles` | Tài khoản và vai trò hiện có |
| `Common` | Repository nền hoặc lớp gọi API dùng chung |

## Theo từng project

- **Application:** `DTOs` vẫn chia theo nghiệp vụ; `DTOs/MenuManagement` chia tiếp
  năm nhóm. `Services` dùng cùng tên nhóm; interface đặt cạnh implementation.
- **Infrastructure:** `Repositories` chia theo nhóm, interface đặt cạnh
  implementation. `Models`, `Data` và `UnitOfWork` giữ nguyên; không đổi schema
  hoặc scaffold lại entity để sắp xếp thư mục.
- **API:** `Controllers` chia `Auth`, `Branches`, `Employees`, `Orders`, `Users`,
  `Roles` và `MenuManagement`. Bảy controller menu ở cùng nhóm, không cần thêm
  một cấp thư mục cho từng file.
- **Web:** `Services` và `Models` chia theo nghiệp vụ. `Services/Common` chứa
  `AuthorizedApiService`. `Components/Pages` chia `Auth`, `Branches`, `Employees`,
  `Orders`, `MenuManagement`, `Home` và `Shared`. Trang `.razor` đi cùng scoped
  `.razor.css`; các component quản lý menu nằm cạnh trang `Menu.razor`.
- **Tests:** test Application nằm trong `Application/<nhóm>`; test token Web
  nằm trong `Web/Auth`.

Namespace C# phản ánh thư mục mới. URL của các trang do `@page` quy định và route
API do attribute quy định, không đổi theo việc di chuyển thư mục. `Routes.razor`
tham chiếu `Pages.Shared.NotFound` theo vị trí mới.

Khi thêm chức năng, đặt file ở nhóm tương ứng thay vì quay lại thư mục gốc.
Không gom nhiều class/interface vào một file chỉ để giảm số lượng file.
