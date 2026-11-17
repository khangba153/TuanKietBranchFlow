# Rà soát khép module nhân viên — 2026-11-17

## Kết luận

Các lỗi kết hợp phát hiện trong audit đã được người dùng duyệt sửa và đã được cải thiện local.
Backend đã chạy regression SQL/HTTP thành công; UI form tạo đã build và kiểm tra HTML chưa đăng nhập,
nhưng chưa kiểm tra tương tác trực quan mới vì browser của chat không kết nối được.
Người dùng đã yêu cầu commit/push phần cải thiện. Kiểm tra tương tác UI mới chưa được
xác nhận riêng; không coi yêu cầu commit/push là bằng chứng browser đã được kiểm tra.

Phần audit bên dưới giữ bằng chứng trước khi sửa. Phần cuối ghi thay đổi và kiểm chứng sau sửa.
Không đổi schema, chưa commit/push/deploy. Probe chỉ nằm trong artifacts (Git ignored), chỉ truy cập SQL localhost.

## Phần đã có và bằng chứng

| Nhóm | Hiện trạng |
| --- | --- |
| Danh sách, tìm kiếm, lọc trạng thái, hồ sơ | Có Web/API, scope chi nhánh tại server |
| Tạo và sửa hồ sơ | Có Web/API; còn lỗi validation/concurrency dưới đây |
| Nghỉ/quay lại | Transaction, thu hồi phiên, giữ lịch sử, chặn same-day/future |
| Chuyển trực tiếp | ADMIN quản lý cả nguồn/đích, transaction và thu hồi phiên |
| Yêu cầu điều chuyển | Gửi/tiếp nhận/từ chối/rút/vô hiệu, lịch sử và unique PENDING |
| Phân quyền | OWNER đọc, ADMIN ghi theo phạm vi, EMPLOYEE không vào quản lý |

- Chạy lại Release build: 0 warning/error; 181/181 unit test đạt.
- HTTP mới trong audit: ADMIN/OWNER đọc employee-list và transfer-list 200; EMPLOYEE đọc hai nhóm 403. OWNER/EMPLOYEE create/edit/status/accept đều 403.
- HTTP trên fixture mới: tạo -> sửa -> nghỉ -> quay lại đều thành công tuần tự.
- Bằng chứng SQL đồng thời/rollback/thu hồi phiên và browser trước đây xem các báo cáo employee-profile-edit, employee-work-status, employee-transfer-workflow. Đây là bằng chứng của các mốc trước, không phải mọi thao tác đã được chạy lại trên browser trong audit này.
- Browser local hiện không kết nối được trong chat; không đánh dấu đã kiểm tra trực quan mới. Bố cục nhóm nút sửa/chuyển gần nhất do người dùng kiểm tra và nghiệm thu.

## Lỗi đã tái hiện trước khi sửa

### P1 — Sửa hồ sơ cũ có thể vô tình bật lại tài khoản đã nghỉ

Source: `EmployeeService.UpdateEmployeeAsync` đọc graph ở khoảng dòng 510, sau đó
`_userRepository.Update(employeeProfile.User)` và `_employeeRepository.Update(employeeProfile)`
ở dòng 603–604 rồi SaveChanges, không giữ cùng khóa với nghiệp vụ status.

Tái hiện bằng service thật, repository thật và SQL local, với một repository con chỉ
tạm dừng kiểm tra mã để điều phối thứ tự (không thay đổi giá trị truy vấn):

1. Request sửa đọc hồ sơ nhân viên đang làm và tạm dừng sau bước đọc.
2. Request nghỉ commit: IsActive=false, LeaveDate=hôm nay, phân công đóng, phiên bị thu hồi.
3. Tiếp tục request sửa: vẫn trả thành công.
4. Đọc lại SQL: IsActive=true, LeaveDate=null, số phân công mở=0.

Hệ quả: thao tác chỉ sửa tên/lương đã ghi đè trạng thái nghỉ. Tài khoản và phân công
không còn nhất quán. Chưa kiểm tra thêm mọi biến thể login sau lỗi; không tuyên bố phiên
cũ đã sống lại (phiên cũ vẫn bị thu hồi).

Đề xuất: mở transaction sửa hồ sơ, giành cùng khóa AppUser trước khi đọc, kiểm tra
lại quyền/phân công sau khóa, chỉ cập nhật trường hồ sơ cho phép; tránh Update cả graph.
Thêm regression test cho edit/status và edit/transfer.

### P1 — Request sửa của admin nguồn vẫn ghi được sau khi chuyển sang ngoài phạm vi

1. ADMIN chỉ có quyền nguồn bắt đầu sửa và đọc hồ sơ.
2. ADMIN quản lý hai nơi chuyển nhân viên sang đích, commit thành công.
3. Request sửa cũ tiếp tục, trả thành công và tên mới được lưu tại đích.
4. Đối chiếu SQL: người sửa không có phân công tại đích (targetAccess=false).

Đây là quyền kiểm tra trước khi thay đổi dữ liệu nhưng không được xác nhận lại tại
thời điểm ghi. Không phải bypass bằng URL thông thường: request mới đến nguồn sau
chuyển vẫn bị chặn. Cần sửa cùng cơ chế transaction/khóa ở mục trên.

### P2 — Validation tạo nhân viên không đồng nhất với sửa hồ sơ

- POST ngày sinh tương lai: HTTP 201 và lưu được, trong khi DTO sửa chặn cùng dữ liệu.
- POST BaseSalary=10000000000000000 (10^16, vượt decimal(18,2)): HTTP 500 thay vì validation 400.
- `EmployeeCreateDTO` chỉ dùng Range(0,double.MaxValue) cho lương và không có kiểm tra
ngày sinh tương lai. DTO sửa đã có giới hạn decimal và kiểm tra ngày.

Đề xuất: dùng chung quy tắc ngày/lương cho create/edit, giữ service kiểm tra cần thiết,
kiểm tra cả Web/API; chưa tự thêm quy tắc tuổi lao động hoặc ngày sinh/ngày vào làm
chưa được người dùng chốt.

### P2 — Tạo trùng đồng thời trả lỗi máy chủ

Hai POST cùng username/mã nhân viên trên fixture chạy song song trả 201 và 500.
SQL unique index vẫn bảo vệ không có hai bản ghi trùng, nhưng kiểm tra Exists trước
SaveChanges không loại bỏ được race. Nhánh thất bại không được chuyển thành 409
với lỗi trường phù hợp.

Đề xuất: giữ unique constraint, bắt đúng lỗi unique khi lưu để trả conflict; không
bắt mọi DbUpdateException rồi gắn nhãn trùng; không retry ghi tự động.

## Điểm cần chốt hoặc cải thiện UX

### Chi nhánh ngừng hoạt động

Fixture chi nhánh IsActive=false nhưng chưa Deleted và admin còn phân công:
POST tạo nhân viên vẫn 201. Code chỉ kiểm tra chi nhánh chưa xóa.

Đề xuất: chi nhánh ngừng hoạt động chỉ đọc lịch sử, không tạo phân công mới hoặc
tiếp nhận/quay lại làm; vẫn cho xử lý nghỉ và chuyển nhân viên ra nơi hoạt động nếu
cần. Cần chốt chính sách trước khi sửa đồng bộ, không tự cấm mọi thao tác làm kẹt
nhân viên tại chi nhánh đóng.

### Trang tạo nhân viên chưa đồng bộ quyền và trạng thái lỗi với các trang mới

- GET Web `/employees/create?branchId=1` khi chưa đăng nhập trả 200 và render form có
username/password. API vẫn yêu cầu ADMIN, nên chưa thấy lỗ hổng tạo nhân viên trái quyền.
Đây là vấn đề UX: người dùng không có quyền có thể điền form rồi mới bị từ chối.
- Trang chưa có NavigationLock cảnh báo bỏ dữ liệu hoặc chặn rời trang khi đang lưu.
- Catch chỉ HttpRequestException; timeout/JSON lỗi chưa được xử lý đồng bộ với form sửa/điều chuyển.
- Sau lỗi mạng thông báo khuyến khích thử lại, không yêu cầu đọc lại danh sách để
kiểm tra thao tác trước có thể đã được lưu.

Đề xuất: chỉ render form cho ADMIN, chặn/giải thích sai phạm vi nguồn, bảo vệ dữ liệu
chưa lưu và thao tác đang gửi, xử lý timeout/JSON/mất phản hồi nhất quán. Giữ Be Vietnam Pro,
control >=44px và không thêm emoji/vector trang trí.

## Thứ tự sửa đề xuất

1. Khóa và cập nhật giới hạn trường của luồng sửa hồ sơ; regression edit/status/edit/transfer.
2. Validation create + ánh xạ unique conflict.
3. Chốt giới hạn ghi ở chi nhánh ngừng hoạt động và áp dụng nhất quán.
4. Hoàn thiện UX form tạo và kiểm tra lại browser khi kết nối khả dụng.
5. Chạy lại happy path, role/scope, SQL concurrency/rollback rồi mới đánh dấu khép module.

Không triển khai kho ở mốc rà soát này. Các fixture đã được dọn theo prefix riêng;
database cloud không bị truy cập hoặc thay đổi.

## Cải thiện đã triển khai sau khi người dùng duyệt — 2026-11-17

### Sửa hồ sơ an toàn

- `UpdateEmployeeAsync` mở transaction, giành khóa AppUser dùng chung với status/transfer/login/order trước khi đọc hồ sơ.
- Sau khóa, kiểm tra lại phân công ADMIN và truy vấn hồ sơ trong phạm vi chi nhánh; request đợi sau khi chuyển không dùng kết quả đọc từ trước.
- Entity được đọc có tracking, chỉ thay đổi thuộc tính hồ sơ cho phép; không gọi Update trên toàn graph. Luồng status cũng bỏ Update graph không cần thiết.
- Mọi kết quả bị từ chối hoặc lỗi lưu đều rollback/clear tracking; không tạo kết quả thành công giả.
- Không tuyên bố ETag/rowversion cho form edit: hai lần sửa thông tin hợp lệ vẫn có thể ghi theo lượt cuối, nhưng không được ghi đè trạng thái/phân công không thuộc form.

### Validation và xung đột

- Tạo/sửa dùng chung `EmployeeProfileValidation.ValidateDates`; chặn ngày vào làm mặc định/tương lai và ngày sinh tương lai.
- Tạo dùng giới hạn decimal(18,2) giống sửa, parse giới hạn theo invariant culture; không thêm quy tắc tuổi hoặc chính sách ngày khác chưa được chốt.
- SQL unique 2601/2627 được phân loại tại Infrastructure. Service chỉ chuyển thành lỗi trùng khi đọc lại xác nhận username/email/code đã trùng; lỗi database khác vẫn được đẩy tới xử lý lỗi chung.
- Create trả 409 khi hai request trùng vượt qua Exists cùng lúc; edit cũng ánh xạ conflict email/code sau rollback. Không retry ghi tự động, không bỏ unique index.

### Chi nhánh ngừng hoạt động — phương án được duyệt

- Không tạo nhân viên mới, không cho nhân viên quay lại tại chi nhánh ngừng hoạt động: API 409 và UI giải thích.
- Vẫn cho nghỉ và chuyển nhân viên ra chi nhánh đang hoạt động; giữ quyền đọc/sửa thông tin lịch sử trong phạm vi hiện có.
- Chuyển/tiếp nhận vào đích ngừng hoạt động vẫn bị workflow transfer chặn như trước.

### Form tạo

- Chỉ render form cho ADMIN; kiểm tra danh mục chi nhánh có quyền và đang hoạt động trước khi cho nhập. Người không có quyền thấy giải thích thay vì form username/password.
- Tái sử dụng input nhận thay đổi ngay khi gõ, có EditContext, cảnh báo bỏ dữ liệu chưa lưu, chặn rời trang và khóa fieldset trong lúc gửi.
- Giữ nhập ngày vào làm dd/MM/yyyy; parse trước khi Validate cả DTO để không bị ngày mặc định chặn submit trước khi parse. Ngày sinh có max hôm nay và validation server.
- Timeout/JSON lỗi/network/response không xác định: khóa gửi lại, hướng dẫn kiểm tra danh sách; không khuyến khích gửi lại mù quáng. Dữ liệu nhập không bị xóa khi validation/conflict.
- Khi đổi chi nhánh qua route sau xác nhận điều hướng: reset model và tải lại quyền, tránh dùng tên/quyền của chi nhánh cũ.
- Giữ bố cục responsive và Be Vietnam Pro; không thêm vector/emoji/trang trí. Thông báo lỗi có focus, fieldset không thêm border mặc định.

## Kiểm chứng sau sửa

- Release build toàn solution và Debug build: 0 warning/error. 191/191 unit test đạt (thêm 10 regression test validation/lock/scope/rollback/inactive branch).
- HTTP fixture: future DOB 400, vượt lương 400, chi nhánh inactive create 409; tạo trùng song song có đúng một 201 và một 409.
- Happy path HTTP create/edit/deactivate/reactivate vẫn thành công.
- SQL điều phối request sửa tạm dừng sau khi đọc: status/transfer phải chờ khóa; edit lưu xong mới tới status/transfer, tài khoản nghỉ vẫn false và LeaveDate giữ hôm nay.
- Sau transfer, request edit từ admin chỉ quản lý nguồn không tìm thấy hồ sơ trong scope, không lưu được.
- HTTP policy inactive: cho nghỉ 200, quay lại 409; chuyển ra đích active 200.
- HTTP Web anonymous `/employees/create?branchId=1`: không còn input username/password, có thông báo chỉ ADMIN được tạo. Đây là kiểm tra HTML SSR, không thay thế browser interaction.
- API/Web Debug đã build và khởi động lại tại cổng 5007/5032 để người dùng kiểm tra bản mới. Restart làm mất phiên RAM Web, cần đăng nhập lại.
- Đã dọn toàn bộ fixture local sau test; không thay đổi schema hoặc database Azure. Người dùng đã duyệt commit/push phần cải thiện; chưa deploy.
- Còn cần nghiệm thu tương tác form tạo: validation cạnh trường, xác nhận bỏ thay đổi, lỗi mạng và mobile bằng browser khi khả dụng; lượt này browser tool báo unavailable.
