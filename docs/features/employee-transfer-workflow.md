# Chuyển chi nhánh nhân viên — phạm vi đã duyệt

Ngày chốt: 2026-11-17.

Trạng thái: ĐÃ TRIỂN KHAI, KIỂM CHỨNG VÀ ĐƯỢC NGƯỜI DÙNG NGHIỆM THU LOCAL ngày 2026-11-17.
Người dùng đã duyệt commit/push; chưa deploy. API cũ chuyển trực tiếp đã được chuyển sang gọi workflow mới,
không còn đường ghi bỏ qua quy tắc mới.

## Hai luồng chuyển

### Cùng phạm vi quản lý

- ADMIN đang có phân công hiệu lực tại cả nguồn và đích được chuyển trực tiếp sau bước xác nhận.
- Không yêu cầu người thứ hai duyệt khi cùng ADMIN có quyền quản lý hai nơi.

### Khác phạm vi quản lý

- ADMIN nguồn gửi yêu cầu cho chi nhánh đích.
- ADMIN có phân công hiệu lực tại đích đồng ý tiếp nhận hoặc từ chối, kèm lý do khi từ chối.
- Trong lúc chờ, nhân viên vẫn ở nguồn, vẫn sử dụng tài khoản và phiên hiện có.
- Chỉ chuyển phân công và thu hồi phiên khi tiếp nhận thành công.
- ADMIN nguồn có thể rút yêu cầu đang chờ. Không xóa yêu cầu để giữ lịch sử.
- OWNER chỉ xem, không được biến thành người phê duyệt thay ADMIN.

## Trạng thái và ngày hiệu lực

- Chờ tiếp nhận: chưa thay đổi phân công.
- Đã chuyển: tiếp nhận và chuyển thành công trong cùng transaction.
- Từ chối: có người xử lý, thời gian và lý do.
- Đã hủy: rút yêu cầu trước khi được xử lý.
- Không còn hiệu lực: nhân viên/phân công/quyền xử lý đã thay đổi khiến không thể chuyển theo yêu cầu cũ.
- Mỗi nhân viên tối đa một yêu cầu chờ, kiểm soát bằng database ngoài kiểm tra service.
- Ngày chuyển là hôm nay khi chuyển trực tiếp hoặc khi tiếp nhận thành công, không dùng ngày gửi yêu cầu.
- Phân công nguồn kết thúc hôm trước, phân công đích bắt đầu hôm nay.
- Không chuyển hồi tố hoặc đặt lịch tương lai trong mốc này.
- Nhân viên phải đang làm ở nguồn; không chuyển người đã nghỉ, vừa bắt đầu phân công hôm nay, còn phân công tương lai hoặc dữ liệu khoảng hiệu lực chồng nhau.
- Chưa đặt thời hạn tự hết hạn theo số ngày; không tự thêm một thời hạn chưa được chốt.

## Phân quyền và phạm vi dữ liệu

- Kiểm tra quyền hiện tại khi gửi và khi xử lý; không tin quyền được lưu từ lúc tạo yêu cầu.
- Khi tiếp nhận phải xác minh người gửi vẫn đủ điều kiện quản lý nguồn và người xử lý có quyền tại đích, cùng tình trạng nhân viên/phân công hiện tại.
- Chỉ công bố danh mục chi nhánh đích tối thiểu phục vụ gửi yêu cầu; không mở quyền đọc hồ sơ, đơn hàng, doanh số của chi nhánh ngoài phạm vi.
- Màn yêu cầu chỉ hiển thị thông tin cần điều chuyển, không mang theo lương hoặc dữ liệu cá nhân không cần thiết.
- Người gửi theo dõi được kết quả, người xử lý, thời gian và lý do.

## Transaction, đồng thời và phiên

- Các thao tác cùng nhân viên dùng chung cơ chế khóa tài khoản với nghỉ/quay lại và login.
- Hai lượt tiếp nhận, tiếp nhận/từ chối, tiếp nhận/hủy hoặc chuyển/nghỉ đồng thời không được gây xử lý hai lần hay phân công nửa chừng.
- Kiểm tra lại dữ liệu sau khi giành khóa, trước khi thay đổi.
- Tạo phân công mới, đóng phân công cũ, cập nhật yêu cầu và thu hồi AuthSession/RefreshToken cùng commit hoặc rollback.
- Tiếp nhận thất bại không được ghi trạng thái Đã chuyển; không làm mất phân công nguồn.
- Sau chuyển cần đăng nhập mới. Phiên/token cũ không sống lại.
- Rà tình huống tạo đơn đang chạy cùng lúc chuyển, không chỉ kiểm tra dropdown hoặc JWT.

## Đơn hàng và lịch sử

- Đơn cũ giữ BranchId và người tạo, không chuyển doanh số sang chi nhánh mới.
- Giữ luồng xem/báo sai đơn do chính nhân viên tạo theo quyền hiện có; không mở quyền xem đơn người khác.
- Lịch sử phân công và yêu cầu chuyển được giữ lại, không xóa hoặc sửa ngược lịch sử để thể hiện chuyển.

## UI và tiêu chí kiểm chứng

- Hồ sơ có thao tác Chuyển chi nhánh, tách khỏi sửa hồ sơ/nghỉ việc.
- Chọn đích, xác nhận nguồn/đích/ngày/hệ quả; thể hiện rõ Chuyển ngay hay Gửi yêu cầu tiếp nhận.
- Màn Yêu cầu đã gửi và Yêu cầu cần tiếp nhận có trạng thái, chi nhánh, thời gian và người xử lý; không tự gửi lại thao tác ghi khi mất phản hồi.
- Sau chuyển trực tiếp chuyển sang hồ sơ ở ngữ cảnh đích; sau gửi vẫn giữ ngữ cảnh nguồn.
- Be Vietnam Pro, nút dễ chạm, không emoji/vector trang trí; kiểm tra mobile/desktop và màn rỗng/lỗi/đang lưu.
- Kiểm chứng bằng unit test, SQL local, HTTP role/scope, browser và các tình huống đồng thời/rollback. Không chạy fixture trên cloud.

## Ranh giới giai đoạn này

- Giữ .NET–Blazor, kiến trúc bốn project, Database First.
- Cần script SQL bổ sung dữ liệu yêu cầu chuyển và mapping tương ứng; không reset database hoặc sửa schema cloud tự động.
- Chưa thêm role admin tổng. Khi CR-001 triển khai có thể mở rộng chính sách duyệt tập trung hoặc kết hợp tiếp nhận; không xây cơ chế cấu hình duyệt tổng quát ngay.
- Không tự commit/push/deploy theo việc duyệt nghiệp vụ này.

## Triển khai thực tế

- SQL `006-add-employee-transfer-request.sql`: bảng yêu cầu, khóa ngoại, rowversion, check constraint và unique filtered index giới hạn một PENDING/nhân viên. Đã áp dụng trên SQL localhost, không ghi cloud.
- SQL `007-grant-employee-transfer-runtime.sql`: cấp SELECT/INSERT/UPDATE cho database user `branchflow_api`, không DELETE. Chưa chạy trên cloud.
- Scaffold riêng bảng mới rồi đưa model/mapping vào `EmployeeTransferRequest` và partial `BranchFlowDbContext.EmployeeTransfers`; không scaffold đè model cũ. FK được database kiểm soát, repository mới dùng các khóa scalar thay vì thêm navigation vào mọi model cũ.
- `EmployeeTransferRepository` lọc phạm vi tại SQL, lấy 20 mục/trang và thêm một mục để biết còn trang sau.
- `EmployeeTransferService` xử lý chuyển trực tiếp, gửi/tiếp nhận/từ chối/rút yêu cầu; snapshot tên/mã nhân viên và chi nhánh để giữ ngữ cảnh lịch sử, không lưu lương vào yêu cầu.
- `EmployeeService` nghỉ/quay lại tự vô hiệu PENDING trong cùng transaction. Các thay đổi quyền hoặc phân công khác được kiểm tra lại khi xử lý yêu cầu; chưa có background job tự quét mọi yêu cầu theo thời gian.
- Endpoint PUT cũ `/api/employees/{id}/current-branch` vẫn trả hồ sơ khi chuyển trực tiếp thành công nhưng đi qua workflow mới và chỉ nhận ngày hôm nay.
- `OrderService` giành cùng khóa AppUser và kiểm tra lại chi nhánh ngay trong transaction trước khi lưu đơn. Đơn giữ khóa trước thì được lưu trước lúc chuyển; chuyển commit trước thì đơn gửi vào chi nhánh nguồn bị từ chối.

### API mới

| Method/route | Quyền | Kết quả |
| --- | --- | --- |
| GET `/api/employee-transfers/targets?sourceBranchId=...` | ADMIN nguồn | Chỉ id/code/name/canTransferImmediately |
| GET `/api/employee-transfers?incoming=false&page=1` | ADMIN/OWNER | Đã gửi theo phạm vi nguồn; OWNER đọc toàn bộ |
| GET cùng route với `incoming=true` | ADMIN/OWNER | Theo phạm vi đích; OWNER đọc toàn bộ |
| POST `/api/employee-transfers` | ADMIN nguồn | Chuyển ngay nếu quản lý cả hai nơi, hoặc tạo PENDING |
| POST `/api/employee-transfers/{id}/accept` | ADMIN đích | TRANSFERRED hoặc INVALIDATED nếu điều kiện cũ không còn |
| POST `/api/employee-transfers/{id}/reject` | ADMIN đích | REJECTED, bắt buộc lý do |
| POST `/api/employee-transfers/{id}/cancel` | ADMIN nguồn | CANCELLED |

- Gửi: `employeeId`, `sourceBranchId`, `targetBranchId`, `transferImmediately`, `note` tối đa 500 ký tự. Server đối chiếu mode với quyền thật, không tự chuyển ngay nếu UI đang xác nhận gửi yêu cầu.
- Xử lý: body `{ reason }`; lý do tối đa 500 ký tự. GET không làm thay đổi trạng thái.
- Thành công 200; dữ liệu sai 400, thiếu xác thực 401, sai quyền 403, không tồn tại 404, đã xử lý/có yêu cầu chờ/điều kiện xung đột 409. Lỗi bất ngờ rollback và đi qua xử lý lỗi chung, không trả stack trace.
- Web không tự retry POST khi mất phản hồi; khóa các thao tác ghi và yêu cầu tải lại trước khi thử tiếp.

## Kiểm chứng đã chạy — 2026-11-17

- Release build toàn solution: 0 warning/error. 181/181 unit test đạt (thêm 21 test nghiệp vụ và 4 test Web client, giữ toàn bộ hồi quy cũ).
- SQL thật: gửi đồng thời chỉ tạo một PENDING; yêu cầu chờ giữ phân công và phiên nguồn; reject/cancel giữ lịch sử và cho phép tạo yêu cầu mới.
- SQL thật: hai accept đồng thời có một thành công/một conflict; một phân công mở tại đích, đúng ngày tiếp nhận, phiên cũ bị thu hồi.
- SQL thật: admin quản lý hai nơi chuyển trực tiếp thành công; tiêm lỗi SaveChanges sau khi thay đổi phân công thì rollback giữ PENDING/nguồn, không để lại phân công đích.
- SQL thật: nghỉ việc vô hiệu PENDING; accept yêu cầu đã vô hiệu bị chặn. Ba lượt accept/nghỉ chạy song song đều giữ nhất quán tài khoản/phân công và trạng thái terminal.
- SQL thật: kiểm tra lại trong transaction tạo đơn từ chối chi nhánh cũ sau khi chuyển.
- HTTP: anonymous GET 401; EMPLOYEE GET 403; OWNER GET 200/POST 403; nguồn accept 403; admin đích truy cập danh mục nguồn không quản lý 403; nguồn đọc hồ sơ ở đích 403. Danh sách đi/đến tách phạm vi; danh mục đích chỉ bốn trường tối thiểu.
- Browser: source gửi PENDING, destination tiếp nhận; từ chối bắt nhập lý do, lưu lý do/người/thời gian và focus thông báo. Admin quản lý cả hai nơi thấy Chuyển ngay, chuyển xong mở hồ sơ ở đích; ghi chú được lưu và hiển thị trong lịch sử.
- Browser mobile 390px, đo 320px không tràn ngang; nút tối thiểu 44px. Desktop 1280px hai cột thông tin. Ảnh local tại `artifacts/employee-transfer-check`.
- Sau phản hồi người dùng, gom Sửa thông tin/Chuyển chi nhánh thành một nhóm căn phải trên desktop, xếp dọc dưới tên/trạng thái trên mobile. Debug Web build sạch; người dùng đã kiểm tra và nghiệm thu bố cục mới trên localhost:5032.
- Fixture chỉ ở localhost, có prefix riêng và đã dọn yêu cầu/phiên/nhân viên/phân công/chi nhánh thử; không sửa dữ liệu cloud hoặc tài khoản thật để điều chuyển.

## Giới hạn vận hành

- Chưa có thông báo realtime/email: admin vào Cần tiếp nhận và bấm tải lại; không thêm dịch vụ thông báo ngoài phạm vi.
- Chưa đặt lịch tương lai, admin tổng hay chính sách duyệt cấu hình theo đơn vị.
- Hôm nay vẫn theo DateTime.Today của host như mốc trạng thái nhân viên; cần chốt riêng chuẩn múi giờ nghiệp vụ trước khi đổi đồng bộ toàn hệ thống.
- Khóa bảo vệ status/transfer/login/create-order và, sau mốc hardening 2026-11-17, cả sửa hồ sơ. Chưa có bảo đảm chống mọi xung đột với thay quyền phân công admin hoặc ETag cho hai form sửa cùng trường; xem employee-module-closeout-audit.md.
- Các request đã bắt đầu xác thực trước lúc thu hồi không phải cơ chế hủy request tức thì. Kiểm tra chi nhánh trong transaction bảo vệ việc ghi đơn vào nguồn sau chuyển; API từ chối phiên cũ ở các request tiếp theo.
- Cần áp dụng 006/007 có kiểm tra môi trường trước khi deploy bản này. Database đã có schema nhưng tiến trình Debug cũ vẫn phải khởi động lại để dùng code mới.
