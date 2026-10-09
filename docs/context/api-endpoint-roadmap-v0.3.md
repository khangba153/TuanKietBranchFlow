# BranchFlow API roadmap v0.3 — một menu chung, seed trước luồng gọi món

Cập nhật: 2026-09-15. Bản này thay thứ tự của [v0.2](api-endpoint-roadmap-v0.2.md) sau khi người học chốt **một menu duy nhất dùng chung cho tất cả chi nhánh**. Đây là roadmap học/triển khai, chưa phải các route đã code hoặc hợp đồng API đã khóa. [v0.1](api-endpoint-roadmap-v0.1.md) và v0.2 giữ để đối chiếu lịch sử.

## Quyết định dữ liệu

- Category, Product, Size, ProductSize/Price, Topping, Note là danh mục và giá dùng chung; không nhân bản menu hoặc giá theo branch. `ProductSize.Id` xác định cặp món–size và giá; order lưu snapshot khi xác nhận.
- Topping thường có `Price = 5.000` cho một phần; quantity 1/2/3 tạo tổng 5K/10K/15K. `UP SIZE 1300ML` là topping đặc biệt giá 10K và quantity phải bằng 1. Giới hạn này thuộc Service vì constraint hiện tại của `OrderItemTopping` mới chỉ bảo đảm Quantity > 0.
- `BranchProduct.IsAvailable` và `BranchTopping.IsAvailable` quyết định riêng từng chi nhánh có đang bán món/topping. ADMIN thao tác availability chỉ tại branch được phân công; EMPLOYEE chỉ đọc món đang bán ở branch hiện tại.
- Trong phạm vi dự án hiện tại chỉ có **một tài khoản ADMIN duy nhất** từ đầu đến cuối. ADMIN này quản lý danh mục/giá menu dùng chung; không thêm role `ADMIN_GLOBAL`/`ADMIN_BRANCH` hoặc cờ quyền riêng ở MVP. Khi thao tác dữ liệu riêng một branch, vẫn phải có phân công `UserBranch` còn hiệu lực tại branch đó. OWNER vẫn read-only. Việc phân cấp hoặc có nhiều ADMIN là thay đổi nghiệp vụ sau dự án hiện tại.
- Quán đã có menu ngoài thực tế nên có thể nhập menu đầu tiên bằng **script dữ liệu ban đầu chạy một lần**. Script của repo hiện chưa seed Category/Product/ProductSize/BranchProduct; dữ liệu có thể đã được nhập thủ công trên DB public, cần kiểm tra trước khi thêm script. Không chạy seed reset trên mỗi startup và không đưa secret vào script.
- Khi tạo branch mới, cần quyết định trạng thái bán ban đầu của từng món/topping; không tự mặc định toàn bộ menu được bán tại branch mới.

## Cách thiết kế một endpoint

Theo quy trình ở [v0.2 §2](api-endpoint-roadmap-v0.2.md): từ hành động thật/prototype → kiểm tra source/schema → chốt route và DTO → role/branch scope → lỗi HTTP → Controller–Service–Repository/UnitOfWork–DbContext → test service/API/SQL → nối Web và smoke test. Seed chỉ thay cách tạo **dữ liệu khởi đầu**, không thay authorization, luật giá, transaction hoặc chức năng CRUD vận hành.

## Thứ tự triển khai đã điều chỉnh

| Mốc | Nội dung | Điều kiện chuyển mốc |
| --- | --- | --- |
| B0 | Baseline Auth/Branch/Employee đã có; kiểm tra quyền và DB demo. | Không lẫn dữ liệu local/public; các test hiện có tiếp tục xanh. |
| B1 | Chuẩn bị menu ban đầu: script nhập Category, Product, >= 2 ProductSize hoạt động/món, Topping/Note và availability cho branch DEMO; xác minh DB. | Có menu hợp lệ để bán; script chạy lại không nhân đôi dữ liệu hoặc được quản lý như script chạy một lần có bằng chứng. Không suy rằng DB public thiếu dữ liệu chỉ từ việc repo thiếu seed. |
| B2 | Gọi món EMPLOYEE: `GET /api/order-menu?branchId`, chọn size/topping/note và giỏ ở Web; `POST /api/orders`, `GET /api/orders/mine`, detail và báo sai. | API kiểm tra branch, active/availability, đọc lại giá DB, lưu snapshot/order nhiều bảng thống nhất; UI desktop/mobile và test quyền/giá/concurrency đạt. |
| B3 | Trang Menu ADMIN đủ 5 tab: Category, Size, Product + ProductSize, Topping và Note; quản lý trạng thái chung và availability từng branch. OWNER xem menu ở chế độ chỉ đọc. | ADMIN duy nhất có thể cập nhật menu chung; branch availability bị giới hạn đúng phân công; OWNER dùng API đọc nhưng mọi thao tác ghi bị từ chối; Web/API/test đạt. Quán thay đổi menu mà không cần sửa script. |
| B3.5 | Thu hồi JWT khi đăng xuất, sau khi hoàn thành B3 và trước B4. | Web xóa token trong trình duyệt; API từ chối token đã đăng xuất dù còn hạn; kiểm tra cả trường hợp dùng lại token cũ và token hết hạn. Chốt cách lưu trạng thái thu hồi và thời gian dọn dữ liệu trước khi triển khai. |
| B4 | Đơn hàng ADMIN/OWNER: lọc/xem detail, ADMIN xử lý báo sai, điều chỉnh/hủy, OWNER chỉ đọc. | Luật trạng thái và audit before/after rõ, quyền 403/404 được kiểm tra; order cũ vẫn giữ snapshot. |
| B5 | Hồ sơ EMPLOYEE read-only rồi Kho/kiểm kho. | Hồ sơ dùng `GET /api/users/me` nếu đủ DTO; kho test tồn không âm, reversal và stocktake trong transaction. |
| B6 | Nhân sự/tài khoản còn thiếu, payroll, dashboard/report; audit theo từng write. | Từng module nối dữ liệu thật, quyền và test; payroll chờ chốt công thức. |
| B7 | Vận hành/portfolio. | CI/CD và release thủ công lặp lại thành công, smoke test, log, backup/restore và giới hạn demo được ghi rõ. |

Không viết cả B2 hoặc B3 rồi mới test: mỗi route/use case vẫn là một lát BE + FE + test. Nếu mục tiêu buổi học là chỉ đọc menu ở màn EMPLOYEE 02, bắt đầu bằng Category Id/Name, Product Id/Name/ImageUrl và ProductSize Id/SizeName/Price; tùy chọn topping/note ở màn 03–04 có thể được trả cùng response hoặc tải khi mở, chưa khóa quyết định.

## Việc quay lại sau khi hoàn thiện Menu

- Xóa Category theo quy trình đề nghị: ADMIN nhập lý do và xác nhận gửi; OWNER xem danh mục/lý do/người gửi/thời điểm rồi xác nhận lần nữa. OWNER từ chối thì Category giữ nguyên và lưu đề nghị ở trạng thái từ chối; OWNER chấp thuận mới xóa mềm (`Category.Deleted = true`). Vì menu dùng chung, quyết định xóa áp dụng toàn hệ thống. Cần lưu đề nghị và lịch sử quyết định riêng, không dùng `Deleted` làm trạng thái chờ OWNER.
- Bổ sung nghiệp vụ xóa nhân viên. API hiện chưa có endpoint xóa nhân viên; trước khi làm phải chốt cách xử lý tài khoản, hồ sơ và lịch sử phân công, không xóa mất dữ liệu lịch sử.
- Hai việc trên được hoãn đến sau khi hoàn thiện các thao tác Menu hiện tại; không làm gián đoạn lát cập nhật Category đang học.

## Tiêu chí Web cho các lát triển khai tiếp theo

- Trước khi làm màn mới, đối chiếu layout, màu, font, khoảng cách, điều hướng và thành phần dùng chung của UI BranchFlow hiện tại; giữ cùng một phong cách cho ADMIN, OWNER và EMPLOYEE thay vì dựng giao diện tạm rồi refactor.
- Hoàn thiện giao diện cùng lát nghiệp vụ: đủ màn danh sách/chi tiết/form cần thiết, quyền thao tác theo role, trạng thái tải/rỗng/lỗi/thành công, xác nhận khi thao tác quan trọng và đường đi từ menu hiện có. OWNER chỉ đọc ở các luồng đã chốt; API vẫn phải bảo vệ quyền ghi.
- Kiểm tra bố cục và thao tác trên desktop lẫn điện thoại, gồm nội dung dài, form, nút và khả năng đọc/chạm. Chỉ coi lát hoàn thành sau khi Web kết nối API thật và smoke test các trạng thái chính; không để phần UI hoàn thiện thành một đợt sửa lại riêng.

## Phụ thuộc và điều chưa chốt

1. **Seed không thay trang ADMIN.** Seed đưa menu hiện hữu của quán vào hệ thống lần đầu; CRUD ADMIN cho phép thêm/sửa/ngừng bán khi menu thực tế thay đổi. Có thể làm B2 trước B3 vì B1 đã tạo dữ liệu thật.
2. **Danh mục chung khác trạng thái bán theo branch.** Sửa tên món/giá sẽ tác động mọi branch; bật/tắt `BranchProduct` chỉ tác động branch được chọn. Không thiết kế `BranchPrice` hoặc bản sao Product per branch với quyết định hiện tại.
3. **Phạm vi ADMIN đã chốt cho dự án hiện tại.** Tài khoản ADMIN duy nhất được sửa danh mục/giá menu chung; thao tác `BranchProduct`/`BranchTopping` vẫn kiểm tra phân công branch. Không suy từ quyết định MVP rằng hệ thống đã có ràng buộc DB ngăn tạo ADMIN thứ hai, hoặc đã thiết kế xong phân cấp ADMIN cho giai đoạn sau.
4. **OWNER xem menu ở chế độ chỉ đọc.** Dùng lại GET của Menu ADMIN và UI read-only; không tạo route OWNER trùng lặp. API phải từ chối mọi yêu cầu ghi của OWNER.
5. **Giá lúc tạo order luôn từ server.** Request giỏ gửi `ProductSizeId`, số lượng và tùy chọn; API đọc lại ProductSize.Price/Topping.Price và kiểm tra IsAvailable. Không dùng giá Web làm nguồn thật.
6. **Order không tự trừ nguyên liệu.** Luồng kho triển khai riêng theo nghiệp vụ MVP; order snapshot vẫn phải được lưu ngay ở B2.

Ở lần phát hành Web thủ công kế tiếp, tiếp tục đối chiếu trạng thái deploy CLI với startup log và HTTPS trước khi tự động CD; site từng chạy public dù một deployment trả `RuntimeFailed`.
