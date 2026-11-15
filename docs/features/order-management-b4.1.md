# B4.1 — Xem đơn hàng cho ADMIN/OWNER

Ngày triển khai local: 2026-11-15. Phạm vi: chỉ đọc danh sách và chi tiết đơn.
Đã khép kiểm chứng local; chưa push/deploy. Đã smoke test bản UX local trên SQL/trình duyệt;
các ca chưa kiểm chứng được ghi riêng bên dưới.

## Những gì đã triển khai

- Menu **Đơn hàng** của ADMIN và OWNER dẫn đến `/orders`.
- Bộ lọc kết hợp: chi nhánh, ngày nghiệp vụ, một phần mã đơn, trạng thái.
- Trạng thái hỗ trợ: `COMPLETED`, `NEEDS_REVIEW`, `ADJUSTED`, `CANCELLED`.
  Đây là bộ lọc trạng thái đã có, không phải bổ sung thao tác điều chỉnh/hủy.
- Phân trang mặc định 20 đơn, sắp xếp thời gian tạo mới nhất rồi ID giảm dần.
  API cho phép PageSize 1–100, Page 1–1.000.000.
- Danh sách có mã đơn, chi nhánh, người tạo, ngày nghiệp vụ/thời gian tạo,
  số lượng món, tổng tiền, trạng thái và liên kết chi tiết.
- Chi tiết có thông tin đơn/người tạo/chi nhánh, snapshot món-size-giá,
  topping và ghi chú; có lý do/người/thời gian báo sai nếu đã lưu.
- Web có trạng thái tải, không có dữ liệu, lỗi và thử lại. Bộ lọc chỉ áp dụng
  khi bấm Tìm; chuyển trang không tự áp dụng các giá trị đang sửa dở.
- Khi đổi ID đơn trong cùng circuit, xóa dữ liệu đơn trước và bỏ response cũ về muộn.

## Quyền và API

Danh tính lấy từ claims JWT đã xác thực, không nhận userId/role từ query.
UI ẩn nội dung cho role khác nhưng kiểm soát thực sự nằm tại API và service.

| API GET | Kết quả |
|---|---|
| `/api/order-management` | Trang danh sách, tổng số kết quả, Page và PageSize |
| `/api/order-management/{orderId}` | Chi tiết trong phạm vi hoặc 404 |

Query mẫu:
`?branchId=2&businessDate=2026-11-15&keyword=CN02&status=NEEDS_REVIEW&page=1&pageSize=20`

- ADMIN: chi nhánh chưa xóa có phân công đang hiệu lực, theo cùng quy tắc
  `DateTime.Today` đang dùng trong BranchService; không tự thêm múi giờ mới.
- OWNER: tất cả chi nhánh chưa xóa. Chi nhánh ngừng hoạt động vẫn có thể xem lịch sử.
- Không có chi nhánh được phép: danh sách rỗng, không truy vấn bảng đơn.
- Chọn chi nhánh tồn tại nhưng ngoài quyền: 403; chi nhánh đã xóa/không có: 404.
- Chi tiết không tồn tại hoặc ngoài quyền: cùng trả 404, không trả dữ liệu đơn.
- Chưa đăng nhập: 401; role không phù hợp: 403 qua middleware authorization.
- Bộ lọc/phân trang sai: 400. API không trả entity hay trường nhạy cảm của AppUser.
- Endpoint EMPLOYEE trong OrdersController giữ nguyên role và hành vi.

## Flow và file

```text
OrderManagement.razor / OrderManagementDetail.razor
  -> OrderManagementApiService -> AuthorizedApiService (token đúng phiên Web)
  -> OrderManagementController (role / HTTP)
  -> OrderManagementService (bộ lọc / phạm vi / ánh xạ snapshot)
  -> OrderRepository (WHERE branchIds + bộ lọc + phân trang, AsNoTracking)
  -> BranchFlowDbContext -> SQL Server
```

Service dùng BranchRepository để xác định quyền hiện tại trước khi đọc đơn.
Repository lọc chi nhánh ngay trong truy vấn SQL, không tải toàn bộ đơn rồi lọc
ở Web. Service còn kiểm tra lại BranchId trên chi tiết trước khi ánh xạ.
Nhánh đọc không gọi Unit of Work/SaveChanges vì không có cập nhật dữ liệu.

- Application: `DTOs/Orders/Management/`, `Services/Orders/OrderManagementService.cs`
  và interface cạnh implementation.
- Infrastructure: hai method mới trong IOrderRepository/OrderRepository,
  kết quả truy vấn phân trang `OrderManagementPage`.
- API: `Controllers/Orders/OrderManagementController.cs` và đăng ký DI.
- Web: ba Razor trong `Components/Pages/Orders/`, API client, NavMenu và DI.
- Tests: `Application/Orders/OrderManagementServiceTests.cs`,
  `Web/Orders/OrderManagementApiServiceTests.cs`.

## Bằng chứng kiểm chứng

- Release build toàn solution: thành công, 0 warning, 0 error.
- `dotnet test TuanKietBranchFlow.Tests/TuanKietBranchFlow.Tests.csproj -c Release --no-build`:
  69/69 test đạt sau closeout, gồm 30 test trước đó, 32 test B4.1 và 7 test hồi quy EMPLOYEE.
- Test mới: danh tính không hợp lệ; ADMIN/OWNER lấy đúng nguồn quyền; branch ngoài
  quyền/không có/không phân công; bộ lọc sai; chuẩn hóa và phân trang; chi tiết
  ngoài quyền; snapshot/tổng lượng/báo sai; URL encoding; Bearer theo phiên;
  giữ HTTP 400/401/403/404, không tự retry HTTP lỗi.
- Đây là unit test với repository/HTTP giả lập; bằng chứng browser/SQL local bổ sung
  được ghi tại cuối tài liệu, không thay kiểm thử toàn diện hoặc trên thiết bị thật.

## Checklist nghiệm thu local (chưa chạy)

Checklist ban đầu; một phần đã được chạy trong UX smoke test bên dưới.

Chỉ dùng database dev và tài khoản thử nghiệm; không seed/reset dữ liệu Azure.

1. ADMIN được gán CN1 nhưng không CN2: danh sách tổng chỉ có CN1; bộ lọc chỉ có
   chi nhánh được phép; tự sửa query sang CN2 trả 403; ID đơn CN2 trả 404.
2. OWNER: xem được đơn CN1/CN2; không có nút chỉnh sửa/hủy hoặc API ghi mới.
3. EMPLOYEE: `/api/order-management` bị 403; tạo/xem/báo sai đơn cá nhân vẫn chạy.
4. Kết hợp bốn bộ lọc, Đặt lại, dữ liệu rỗng, nhiều hơn 20 đơn và trang cuối.
   Đổi filter nhưng chưa Tìm rồi bấm Sau: vẫn dùng bộ lọc đã áp dụng.
5. Mở chi tiết đơn đã báo sai; đối chiếu snapshot, số lượng và tổng tiền với SQL.
   Đổi menu hiện tại trên dev không làm đổi snapshot đơn cũ.
6. Đổi phân công hết hiệu lực, refresh danh sách/chi tiết: không còn xem dữ liệu
   chi nhánh cũ. Kiểm tra branch ngừng hoạt động và branch đã xóa.
7. Test 401/hết phiên, API mất kết nối, Thử lại và chuyển nhanh giữa các ID đơn.
8. Kiểm tra desktop/mobile: menu, form, cuộn bảng ngang, nội dung dài và dấu tiếng Việt.

## Không nằm trong B4.1

Không sửa/hủy đơn, duyệt báo sai, thêm audit nghiệp vụ, đổi schema/scaffold,
thêm tenant/role/QR/pha chế; không cài package mới, chạy SQL cloud hoặc deploy.
Các phần ghi dữ liệu cần đặc tả trạng thái và quyền riêng trước khi triển khai.

## UX refinement — đã duyệt 2026-11-15

- Dưới 992px dùng thẻ đơn thay bảng nhiều cột. Mã, trạng thái, chi nhánh,
  ngày, người tạo, số món, tổng tiền và nút xem hiển thị không cần kéo ngang.
- Từ 992px giữ bảng gọn hơn: gộp chi nhánh/người tạo vào một cột.
- Tìm mã nằm ngoài; nút Bộ lọc mở vùng chi nhánh/ngày, Áp dụng và Đặt lại.
  Phạm vi đã áp dụng luôn có nhãn. Bộ chọn trạng thái chuyển thành các nút có số đếm.
- Bốn ô tổng hợp xếp 2×2 trên mobile, một hàng ở desktop rộng: tổng đơn,
  giá trị đơn hoàn tất, chờ xem lại, đã hủy. Repository tính tổng tại SQL,
  trước Skip/Take và trước lọc status; branch/ngày/mã và quyền vẫn được áp dụng.
  Do đó chọn status chỉ đổi danh sách/TotalCount, không đổi toàn cảnh Summary.
- CompletedOrderValue cộng TotalAmount của COMPLETED và ADJUSTED; loại NEEDS_REVIEW
  và CANCELLED. Không coi đây là tiền thực thu hoặc báo cáo doanh thu đã đối soát.
- Summary đi cùng response danh sách, không có endpoint ghi hay schema mới.
  Tổng hợp/count/danh sách là các truy vấn đọc liên tiếp, không phải snapshot
  transaction thống nhất khi dữ liệu đang thay đổi đồng thời.
- Chi tiết ưu tiên tổng tiền/số món, báo sai rồi món; metadata chuyển xuống cuối.
  Nhãn Chỉ xem nhỏ thay banner lớn; tiền dùng định dạng vi-VN.
  Input date native vẫn có thể theo locale trình duyệt dù trang dùng lang vi-VN.
- Mobile ADMIN/OWNER bỏ hàng topbar tài khoản riêng, chuyển Đăng xuất vào menu
  đang thu gọn. Form POST và AntiforgeryToken giữ nguyên; EMPLOYEE không đổi.
- Control chính cao tối thiểu 44px. Không thêm package, JavaScript hay đổi stack.
- Thêm test ánh xạ/deserialize tổng hợp độc lập với trang hiện tại và status.

### Kiểm tra bổ sung

1. So sánh Summary giữa các trang và khi chọn từng trạng thái: giữ nguyên với
   cùng branch/ngày/mã. TotalCount/Items phải theo status được chọn.
2. Đổi branch/ngày/mã rồi Áp dụng: số liệu tổng hợp chỉ thuộc phạm vi mới.
   ADMIN không có quyền không nhận được Summary của chi nhánh khác.
3. Có COMPLETED/ADJUSTED/NEEDS_REVIEW/CANCELLED: đối chiếu tổng tiền theo công thức
   trên; dữ liệu rỗng có các số 0, không lỗi SQL aggregate.
4. Kiểm tra 320/390/768/1440px, không tràn ngang toàn trang, nút xem dễ chạm,
   menu Đăng xuất mở được, bộ lọc đóng/mở và trạng thái được chọn rõ ràng.

### Bằng chứng smoke test local — 2026-11-15

- Agent khởi động API/Web bản Release local. Người dùng cho phép đăng nhập bằng
  tài khoản dev; không lưu mật khẩu trong source, probe, context hoặc tài liệu.
- Đã đăng nhập OWNER và ADMIN, mở `/orders`, mở/đóng bộ lọc, Áp dụng/Đặt lại,
  chọn trạng thái, mở chi tiết đơn báo sai, đăng xuất bằng menu mobile.
- OWNER: 18 đơn, 13 COMPLETED, 5 NEEDS_REVIEW, 0 ADJUSTED/CANCELLED;
  CompletedOrderValue = 649.000đ. Chọn NEEDS_REVIEW còn 5 kết quả, Summary giữ nguyên.
- Chọn CN01 không có đơn: Summary và số kết quả đều 0, không lỗi aggregate SQL.
- ADMIN lọc ngày 07/11/2026: 1 đơn chờ xem lại, giá trị hoàn tất 0đ.
  Tìm mã không khớp: 0; nhập `20261107` và Enter: 1; Đặt lại về 18.
- Probe SELECT local độc lập đối chiếu SQL: COMPLETED 13 / 649.000đ,
  NEEDS_REVIEW 5 / 423.000đ. Probe từ chối database không ở localhost và không ghi
  business data. File probe trong artifacts bị ignore, không nằm trong commit.
- Đã quan sát ở 320/390/768/1440px; DOM đo chiều rộng trang không vượt viewport.
  Mobile thấy thẻ và nút xem; desktop thấy bảng. Chưa kiểm tra điện thoại vật lý.
- Sau test trả bộ lọc về mặc định, bỏ viewport override. API/Web local còn chạy
  để người dùng nghiệm thu. Không deploy Azure.
- Chưa có dữ liệu >20 đơn để test pagination browser; chưa có dữ liệu ADJUSTED/
  CANCELLED thực tế; chưa kiểm chứng hồi quy EMPLOYEE, thu hồi phân công hoặc
  bypass HTTP ngoài quyền bằng browser ở đợt UX này. Unit test quyền vẫn đạt.
- Khi restore probe, NuGet báo NU1902/NU1903 cho dependency bắc cầu
  System.Security.Cryptography.Pkcs 9.0.11. Không đổi package trong scope UX;
  cần rà soát dependency ở đầu việc riêng. Release build solution --no-restore
  vẫn 0 warning/error; không xem build xanh là bằng chứng không có lỗ hổng.

### Khép B4.1 — 2026-11-15

- Người dùng tạm chấp nhận UX; không coi đây là thiết kế cuối hoặc duyệt toàn bộ B4.2.
- Thêm 7 test hồi quy đọc đơn/snapshot/quyền sở hữu/báo sai của EMPLOYEE;
  các test tạo đơn và transaction trước đó vẫn chạy. Tổng 69/69 đạt.
- Release build sau dừng host local đang giữ DLL: 0 warning/error. Lần build đầu
  có warning file lock vì host đang chạy; không phải lỗi compile, đã build lại sạch.
- SQL integration probe tạo 25 header đơn tạm trong transaction, kiểm tra trang
  20+5, không trùng ID, thứ tự CreatedAt rồi Id, tổng hợp không phụ thuộc trang/status,
  và branch ngoài phạm vi trả rỗng. Rollback và SELECT xác nhận không giữ lại đơn thử.
  Identity có thể tăng và để khoảng trống; không reseed. Probe đầu thiếu metadata báo sai
  bị CK_SalesOrder_Review chặn; fixture đã sửa đủ metadata, lần chạy sau đạt.
- Browser EMPLOYEE: đăng nhập, menu 63 món, chọn size/thêm món vào giỏ,
  xóa món thử khỏi giỏ, danh sách/chi tiết đơn cá nhân và chặn màn quản lý.
  Không gửi đơn mới hoặc báo sai lên bản ghi thật bằng browser trong đợt này.
- HTTP local dùng đăng nhập thật: anonymous GET quản lý 401; EMPLOYEE GET quản lý
  403 và GET đơn cá nhân 200; OWNER/ADMIN GET quản lý 200, POST tạo đơn EMPLOYEE 403.
  Chỉ phát sinh phiên auth thử; không ghi đơn nghiệp vụ. Không lưu password/token.
- Pkcs ghim 9.0.21; restore và audit cả solution không còn package có lỗ hổng
  theo nguồn hiện tại. Xem docs/security/package-audit-20261115.md.
- Chưa test browser phân trang với >20 đơn nhìn thấy đồng thời (đã test repository
  trên SQL thật 25 đơn), điện thoại vật lý, hoặc toàn bộ tình huống đồng thời.
- Phần điều chỉnh/hủy/xử lý báo sai của ADMIN vẫn chờ đặc tả và duyệt riêng.
