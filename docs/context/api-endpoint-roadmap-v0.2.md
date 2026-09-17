# BranchFlow API roadmap v0.2 — menu ADMIN trước, order EMPLOYEE sau

> Thứ tự đã được điều chỉnh sau khi chốt một menu chung và seed dữ liệu đầu tiên: xem [API roadmap v0.3](api-endpoint-roadmap-v0.3.md). Bản v0.2 giữ để đối chiếu các route và quyết định cũ.

Cập nhật: 2026-09-15. Đây là thứ tự thiết kế và triển khai được điều chỉnh theo yêu cầu người học, không phải danh sách route đã code hoặc hợp đồng API đã khóa. Giữ [roadmap v0.1](api-endpoint-roadmap-v0.1.md) để đối chiếu lịch sử; số lượng 81 endpoint của bản cũ không còn là chỉ tiêu vì bản cũ thiếu thao tác riêng cho Topping và NoteOption. Mọi module tiếp tục theo flow Web → Controller → Service → Repository/UnitOfWork → DbContext → SQL.

## 1. Vì sao đổi thứ tự

- Prototype ADMIN 13–18 có đủ tab Sản phẩm, Danh mục, Size, Topping và Ghi chú nhanh. Form sản phẩm gắn Category, ít nhất hai size/giá, trạng thái trong menu và trạng thái bán tại chi nhánh.
- Prototype EMPLOYEE 02 chỉ hiển thị danh mục, thẻ món và giá bắt đầu; 03–04 mới mở size, topping, note, số lượng. Giỏ chỉ tồn tại trong Web cho tới lúc xác nhận tạo order.
- Category/Product/ProductSize/BranchProduct chưa có seed trong script của repo; API hiện không có controller menu. Màn EMPLOYEE cần một đường ADMIN tạo dữ liệu menu thật trước khi đọc.
- Prototype OWNER không có trang menu riêng. OWNER luôn chỉ đọc; nếu cần trang menu OWNER, dùng lại GET của ADMIN và giao diện read-only, không tạo endpoint OWNER trùng lặp. Việc thêm trang này còn cần người học xác nhận.
- Hồ sơ EMPLOYEE ở prototype 18 đã có phần lớn dữ liệu từ GET /api/users/me hiện tại; đây có thể là một lát UI ngắn trước kho, không phải một cụm API mới bắt buộc.

Thứ tự mới ưu tiên hoàn thiện cụm menu ADMIN/OWNER trước luồng order EMPLOYEE như người học chọn. Bên trong từng cụm vẫn làm từng lát nhỏ BE + FE + test; không viết cả cụm API rồi mới kiểm tra UI/runtime.

## 2. Quy trình thiết kế cho từng endpoint

1. Chỉ ra người dùng, hành động và dữ liệu thật từ nghiệp vụ/prototype; phân biệt màn hình, popup, bộ lọc và nút UI với use case. Không mặc định mỗi popup cần một route.
2. Kiểm tra route/DTO/service/repository/schema đã có. Ghi “đã code”, “dự kiến” hoặc “cần sửa” theo source hiện tại; không suy ra API hoàn thành từ ảnh prototype.
3. Chốt tài nguyên và HTTP method. GET đọc, POST tạo hoặc thực hiện hành động nghiệp vụ, PUT cập nhật nội dung, PATCH đổi một trạng thái/phạm vi. Tách trạng thái hoạt động khỏi soft delete/restore.
4. Viết input/output trước code: route/query/body, DTO vừa đủ cho màn hình, ID ổn định, tên/giá cần hiển thị, dữ liệu không được trả. Giá/tổng tiền từ Web chỉ là tham khảo.
5. Chốt danh tính, role và branch scope tại API. OWNER chỉ đọc toàn bộ; ADMIN ghi trong quyền được chốt; EMPLOYEE chỉ làm việc tại chi nhánh hiện tại hoặc dữ liệu của mình. Không dùng ẩn nút Web làm ranh giới bảo mật.
6. Liệt kê nhánh lỗi và HTTP status: 400 input, 401 thiếu/không hợp lệ token, 403 thiếu quyền, 404 tài nguyên ngoài scope/không tồn tại, 409 xung đột nghiệp vụ khi phù hợp. Không lộ secret/stack trace.
7. Vẽ flow Controller → Service → Repository/UnitOfWork → DbContext. Query/filter ở repository; luật nghiệp vụ ở service; nhiều entity phải thay đổi cùng nhau thì lưu thống nhất trong transaction.
8. Đặt test trước khi coi hoàn thành: luật service, 401/403/404/409, scope chi nhánh, dữ liệu đã xóa/ngừng bán, transaction/concurrency khi có ghi nhiều bảng. Phân biệt unit test với integration/API/SQL test.
9. Nối đúng màn Web, test desktop/mobile và public smoke test bằng dữ liệu demo; chạy CI. Ghi rõ build xanh, test xanh, Web chạy public và deploy command xanh là bốn bằng chứng khác nhau. Tự động CD chỉ sau khi đường phát hành thủ công lặp lại ổn định.

## 3. Thứ tự module và điều kiện qua mốc

| Mốc | Luồng hoàn thiện | Điều kiện qua mốc |
| --- | --- | --- |
| B0 | Auth, branch scope, nhân viên hiện có | Giữ baseline đã test; bổ sung bài test quyền âm và token bị vô hiệu trước public ổn định. Không viết lại endpoint đã có chỉ vì đổi roadmap. |
| B1 | Menu ADMIN; OWNER đọc nếu có trang menu | Hoàn thiện thao tác thật của 5 tab ADMIN, một Product có >= 2 size/giá và trạng thái bán theo branch; OWNER không thể ghi; API/Web/test chạy với dữ liệu thật. |
| B2 | Gọi món EMPLOYEE và order của tôi | EMPLOYEE đọc được menu branch được phân công, chọn size/topping/note, giữ giỏ ở Web, tạo order từ giá API kiểm tra lại và xem order của mình. |
| B3 | Đơn hàng ADMIN/OWNER | ADMIN xem theo branch, xử lý báo sai/điều chỉnh/hủy đúng luật; OWNER dùng chung GET để xem mọi branch, mọi write trả 403. |
| B4 | Hồ sơ cá nhân EMPLOYEE | Đối chiếu GET /api/users/me với prototype, làm UI read-only và liên kết “Đơn của tôi”; không thêm endpoint chỉ vì thêm trang. |
| B5 | Kho/kiểm kho | ADMIN/EMPLOYEE/OWNER làm đúng role và branch; tồn không âm, giao dịch sai dùng reversal, stocktake lưu lịch sử và cập nhật tồn trong transaction. |
| B6 | Nhân sự/tài khoản còn thiếu, payroll, dashboard, báo cáo | Từng module có luật, DTO, test và UI riêng; payroll chờ chốt công thức, KPI đọc dữ liệu thật. Audit đi cùng các nghiệp vụ ghi, không để cuối mới bổ sung. |
| B7 | Vận hành và portfolio | Bản public demo có CI/CD đã chạy thật, release/smoke test, xử lý lỗi, log, backup/restore và giới hạn được ghi rõ. |

Hồ sơ EMPLOYEE có thể làm sớm sau B2 hoặc B3 nếu cần một màn nhỏ để hoàn thiện tab Cá nhân; thứ tự mặc định ở đây là sau B3 và trước kho như người học đề xuất. Không đợi hoàn thành B7 mới tiếp tục endpoint mới: kiểm thử và vận hành tối thiểu được thêm sau mỗi lát.

## 4. B1 — Menu ADMIN/OWNER trước

Làm theo dữ liệu nền rồi dữ liệu bán: Category → Size → Product + ProductSize → BranchProduct → ToppingGroup/Topping + BranchTopping → NoteGroup/NoteOption. Size/ToppingGroup/NoteGroup/NoteOption có seed nền trong script; đó không thay thế quyền tạo/sửa từ UI ADMIN. Các route dưới đây là đề xuất để phủ hành động thật của 5 tab, chưa khóa chi tiết body/status.

| Tab | Route đọc dùng chung cho ADMIN/OWNER | Route ghi của ADMIN |
| --- | --- | --- |
| Danh mục | GET /api/categories | POST /api/categories; PUT /api/categories/{id}; PATCH /api/categories/{id}/status |
| Size | GET /api/sizes | POST /api/sizes; PUT /api/sizes/{id}; PATCH /api/sizes/{id}/status |
| Sản phẩm | GET /api/products?branchId={id}; GET /api/products/{id}?branchId={id} | POST /api/products; PUT /api/products/{id}; PATCH /api/products/{id}/status; PATCH /api/products/{id}/branches/{branchId}/availability |
| Nhóm topping | GET /api/topping-groups (kèm topping) | POST /api/topping-groups; PUT /api/topping-groups/{id}; PATCH /api/topping-groups/{id}/status |
| Topping | Cùng response nhóm; có thể thêm GET /api/toppings khi UI cần lọc riêng | POST /api/toppings; PUT /api/toppings/{id}; PATCH /api/toppings/{id}/status; PATCH /api/toppings/{id}/branches/{branchId}/availability |
| Nhóm note | GET /api/note-groups (kèm option) | POST /api/note-groups; PUT /api/note-groups/{id}; PATCH /api/note-groups/{id}/status |
| Note option | Cùng response nhóm | POST /api/note-options; PUT /api/note-options/{id}; PATCH /api/note-options/{id}/status |

POST/PUT Product nhận các lựa chọn size và giá cùng request; Service kiểm tra ít nhất hai ProductSize hoạt động và lưu Product + ProductSize thống nhất. Không tạo route POST/PUT ProductSize riêng nếu form không có use case độc lập. Product/Category/Size/Topping/Note có trạng thái chung; BranchProduct/BranchTopping có availability riêng theo branch. Filter của GET ADMIN có thể hiển thị cả món đang tạm ngưng; GET EMPLOYEE sau này chỉ trả món hợp lệ để bán. OWNER đọc được thông tin cần xem nhưng không thấy nút/route ghi.

Soft delete/restore của master data là yêu cầu nghiệp vụ nhưng prototype 13–18 chủ yếu thể hiện trạng thái hoạt động/tạm ngưng. Trước khi thêm DELETE hoặc restore route, chốt UI/hành vi và ảnh hưởng đến order cũ; không coi PATCH status là soft delete. Upload ảnh cũng cần quyết định nơi lưu; ImageUrl có sẵn trong model nhưng endpoint upload chưa được chốt.

Điều kiện bảo mật chưa chốt: Category, Size, Product, Topping và Note là danh mục dùng chung giữa các chi nhánh. Một ADMIN chỉ được phân công chi nhánh A có được sửa dữ liệu chung ảnh hưởng B không? Không tự gán quyền toàn doanh nghiệp cho mọi ADMIN. Availability theo branch bắt buộc kiểm tra ADMIN có quyền tại branch đích; OWNER chỉ đọc.

## 5. B2 — EMPLOYEE gọi món và tạo order

| Hành động | Route dự kiến | Ghi chú |
| --- | --- | --- |
| Màn gọi món 02 và tùy chọn 03–04 | GET /api/order-menu?branchId={id} | Category, món đang bán, ProductSize/giá, topping đang bán, note hợp lệ. Quyết định một response hay tải tùy chọn khi mở sau khi đo/test UI; không cần route chỉ vì popup. |
| Xác nhận giỏ | POST /api/orders | Body gồm ProductSizeId, Quantity, ToppingId/quantity, NoteOptionId theo nhóm; không tin giá/tổng từ Web. API xác minh lại giá, active/availability/scope, tạo snapshot và lưu order cùng item/note/topping trong transaction. |
| Lịch sử và chi tiết của tôi | GET /api/orders/mine; GET /api/orders/{id} | EMPLOYEE chỉ xem order do mình tạo; OWNER/ADMIN dùng cùng GET detail nhưng phạm vi khác. |
| Báo sai | POST /api/orders/{id}/report-error | EMPLOYEE báo lỗi theo điều kiện trạng thái; không trực tiếp sửa order đã lưu. |

Tìm món, chọn size/topping/note, tăng giảm số lượng và chỉnh giỏ trước xác nhận là state Web. Mục “Món thường gọi 30 ngày gần nhất” trên prototype 02 phụ thuộc lịch sử order; xem xét query/route đọc riêng sau khi có order thật, không đưa dữ liệu giả vào GET menu. Mã order theo branch/ngày/sequence phải chống trùng khi đồng thời; snapshot tên/giá giữ lịch sử khi menu đổi. Không tự trừ nguyên liệu khi bán vì ngoài MVP.

## 6. B3 — ADMIN xử lý order; OWNER xem lại

| Hành động | Route dự kiến | Quyền |
| --- | --- | --- |
| Danh sách theo branch/ngày/trạng thái/employee và chi tiết snapshot | GET /api/orders; GET /api/orders/{id} | ADMIN chỉ branch được phân công; OWNER toàn bộ branch và chỉ đọc. Dùng lại route B2, không nhân bản endpoint OWNER. |
| Điều chỉnh báo sai | POST /api/orders/{id}/adjustments | ADMIN trong scope; lưu lý do, người xử lý, before/after và giá trị mới thống nhất. |
| Hủy order | POST /api/orders/{id}/cancel | ADMIN trong scope; check chuyển trạng thái và doanh thu, không xóa dòng order. |

Danh sách trạng thái và luật chuyển COMPLETED/NEEDS_REVIEW/ADJUSTED/CANCELLED còn cần chốt trước khi code write. Owner không có POST/PUT/PATCH; giao diện read-only và API phải trả 403 khi cố ghi. Một báo sai/điều chỉnh phải audit cùng lúc, không chỉ đổi nhãn trên Web.

## 7. B4 — Hồ sơ EMPLOYEE trước kho

GET /api/users/me đã code và UserProfileDTO hiện có FullName, EmployeeCode, HireDate, Phone, Email, Address và CurrentBranchName để đối chiếu prototype EMPLOYEE 18. Nối UI đọc hồ sơ và kiểm tra tài khoản ở hai trình duyệt không lẫn dữ liệu. EMPLOYEE không tự sửa hồ sơ nhân sự trong MVP; không thêm PUT /api/users/me chỉ vì có màn hồ sơ. Đổi mật khẩu là use case tài khoản riêng, chưa phải điều kiện của màn read-only.

## 8. B5 — Kho/kiểm kho

Giữ các nhóm route của v0.1, triển khai theo thứ tự dữ liệu nền → tồn → giao dịch → kiểm kho:

- ADMIN cấu hình Ingredient và IngredientUnit: GET/POST/PUT/PATCH status cho /api/ingredients và /api/ingredient-units.
- Các vai trò đọc tồn theo scope bằng GET /api/inventory?branchId={id}; OWNER đọc mọi branch.
- GET /api/stock-transactions và GET /api/stock-transactions/{id} cho lịch sử; POST /api/stock-transactions tạo IN của ADMIN hoặc OUT của EMPLOYEE theo quyền và scope. POST /api/stock-transactions/{id}/reverse chỉ ADMIN, tạo REVERSAL mới thay vì sửa giao dịch cũ.
- GET /api/stocktakes và GET /api/stocktakes/{id} cho lịch sử; POST /api/stocktakes hoàn tất một lần, kiểm tra chênh lệch và cập nhật tồn trong transaction. EMPLOYEE/ADMIN thao tác theo luật đã chốt; OWNER chỉ đọc.

Test tồn không âm, nhiều dòng giao dịch, đồng thời xuất kho, reversal, stocktake một phần và branch scope trước khi coi mốc qua. Order không tự động trừ Ingredient.

## 9. B6–B7 — Các phần còn lại, không bị xóa khỏi MVP

- Các API nhân viên hiện đã có GET list/detail, POST, PUT, đổi branch và PATCH status. Tiếp tục UI ADMIN ghi/OWNER xem; bổ sung negative tests, tài khoản bị khóa và ngày hiệu lực. Không thiết kế lại sáu route đó từ đầu.
- Account/Branch còn thiếu theo v0.1: admin quản lý /api/users và /api/branches theo quyền; GET /api/roles đã có. GET /api/users/me đã có; đổi mật khẩu, reset mật khẩu và trạng thái account là use case riêng, phải kiểm tra token còn hạn sau khi khóa user.
- Payroll: GET /api/payrolls, GET /api/payrolls/{id}, GET /api/payrolls/mine, POST /api/payrolls, PUT /api/payrolls/{id}, PATCH /api/payrolls/{id}/paid chỉ sau khi chốt công thức và quyền xem lương. Unique employee/tháng và snapshot cần test.
- Dashboard/report: GET /api/dashboard/employee, GET /api/dashboard/management, GET /api/reports/operations. Dữ liệu/order/kho/payroll phải có thật; không dùng seed demo để mô tả KPI kinh doanh thật.
- Audit: GET /api/audit-logs và GET /api/audit-logs/{id} dành cho người có quyền. Ghi audit đi cùng các POST/PUT/PATCH quan trọng ở B1–B6, không chờ B6 mới bắt đầu ghi.
- Mỗi release giữ script Database First có thứ tự, CI test, artifact cũ, smoke test và cách xem log/rollback. Formal Web deploy từng báo RuntimeFailed dù site phục vụ được; ở lần phát hành thủ công kế tiếp kiểm tra liệu lặp lại trước CD, không tự coi một lần test public là quy trình phát hành sạch.

## 10. Quyết định phải chốt trước khi khóa contract

1. ADMIN quản lý danh mục menu dùng chung toàn doanh nghiệp theo quyền nào? Owner read-only đã chốt; không tự cấp quyền toàn cục cho ADMIN của một branch.
2. UI OWNER có cần trang Menu read-only không? Prototype OWNER hiện không có; route GET có thể tái sử dụng.
3. “Ngừng hoạt động”, soft delete và restore của Category/Product/Size/Topping/Note khác nhau ra sao? Product đang dùng trong order lịch sử không được xóa snapshot.
4. Ảnh sản phẩm lưu tại đâu và ImageUrl được tạo như thế nào? Không thêm endpoint upload cho đến khi chọn nơi lưu.
5. GET order-menu trả toàn bộ lựa chọn hay chỉ màn 02 rồi tải tùy chọn 03–04 khi mở? Chọn theo kích thước response và trải nghiệm thực tế; không chia route theo popup một cách máy móc.
6. Trạng thái và chuyển trạng thái order, phạm vi ADMIN điều chỉnh/hủy, giá sau điều chỉnh và audit before/after.
7. Công thức payroll và quyền xem dữ liệu lương nhạy cảm trước B6.

Không cần chốt cả bảy câu ngay. Câu 1 là điểm dừng trước endpoint menu ADMIN đầu tiên; các câu sau được chốt trước use case tương ứng.
