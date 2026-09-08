# BranchFlow: lộ trình học, hoàn thiện và đưa lên production

Cập nhật: 2026-09-05. Đây là kế hoạch đề xuất từ mục tiêu người học, không phải báo cáo đã triển khai. Nghiệp vụ chuẩn vẫn ở [PROJECT_CONTEXT](../../PROJECT_CONTEXT.md) và [business requirements](../context/business-requirements-v1.5.md).

## 1. Hiểu đúng yêu cầu

Đích đến không chỉ là một đường link chạy được. Người học cần có một sản phẩm portfolio và tự trả lời được: hệ thống chạy ở đâu, dữ liệu ở đâu, ai được làm gì, cập nhật phiên bản thế nào, tìm lỗi ở đâu và khôi phục thế nào.

Không mặc định người học hiểu CI/CD, hosting, Docker hoặc JWT chỉ vì đã dùng lệnh mẫu. Với mỗi chủ đề, đi qua bốn mức: biết mục đích -> giải thích bằng BranchFlow -> tự làm có hướng dẫn -> tự kiểm chứng và xử lý một lỗi nhỏ. Ghi nhận theo bằng chứng, không theo số lần nghe giải thích.

“Deploy vertical” ở đây được hiểu là **triển khai một vertical slice**: làm trọn một luồng nhỏ qua UI, API và database rồi đưa luồng đó lên môi trường từ xa. Đây không phải một công nghệ deploy mới, không yêu cầu đổi sang Vertical Slice Architecture hoặc microservices.

## 2. Điểm xuất phát đã kiểm tra từ source

| Hạng mục | Bằng chứng và giới hạn |
| --- | --- |
| Kiến trúc | Bốn project .NET 10; API -> Application -> Infrastructure; Web tham chiếu Application để dùng DTO. Không có Domain riêng. |
| Backend | Có 11 action trong Auth, Users, Branches, Roles, Employees controllers. Đây là số action đã khai báo, không khẳng định mọi hành vi production đã đúng. |
| Xác thực | API có JWT Bearer, PasswordHasher, DI; các bài test Swagger trước đây là kết quả người học báo đã đạt, chưa có bộ regression tự động chứng minh. |
| Nhân viên | Có danh sách, chi tiết, tạo, sửa, chuyển chi nhánh. Đổi trạng thái mới có DTO/interface/repository hỗ trợ, chưa có implementation service và action controller. |
| Frontend | `Web/Program.cs` đăng ký Interactive Server; `Components/Pages/Home.razor` còn Hello world. Chưa có luồng login–nhân viên thực tế trong source Web. |
| Vận hành | Chưa tìm thấy workflow CI, project test, Dockerfile ứng dụng hoặc cấu hình deploy trong repo khi kiểm tra. SQL Docker do người học mô tả, không suy ra app đã được container hóa. |
| Production API | Swagger hiện chỉ bật ở Development. Cần bổ sung/kiểm tra xử lý lỗi production, logs và health check trước khi public; không bật Development trên cloud để có Swagger. |

Trạng thái dễ thay đổi nằm ở [CURRENT_STEP](CURRENT_STEP.md). Không ước lượng phần trăm hoàn thành từ số endpoint hoặc số bảng.

## 3. Những khái niệm phải học gắn với dự án

| Chủ đề | Hiểu bằng ví dụ BranchFlow | Khi học sâu / cách chứng minh |
| --- | --- | --- |
| Build / publish / deploy | Build kiểm tra và biên dịch; publish tạo bộ file để chạy; deploy đưa bản đó cùng cấu hình tới nơi chạy. | Trước deploy thử: chạy bản publish ngoài IDE. |
| Local / staging / production | Local để code; staging để thử bản phát hành; production là bản public đã chọn để người khác dùng. | Tách cấu hình và dữ liệu, chỉ ra đang kết nối DB nào. |
| Hosting / VPS / container | Hosting chạy ứng dụng; VPS là máy mình phải quản lý nhiều hơn; container đóng gói môi trường, không tự cung cấp server. | Chọn một cách host phù hợp Interactive Server và ngân sách. |
| Domain / DNS / HTTPS | Tên truy cập, cách tìm máy phục vụ, kênh truyền mã hóa là ba việc khác nhau. | Dùng hostname có sẵn của nền tảng trước; domain riêng không là điều kiện làm MVP. |
| Secrets / environment | JWT key, DB credential khác nhau giữa local và cloud; User Secrets local không tự đi theo bản publish. | Thiếu một cấu hình trên môi trường thử: đọc log và sửa đúng nơi, không in giá trị secret. |
| CI | Máy build độc lập tự kiểm tra mỗi thay đổi Git. | Một thay đổi sai làm CI đỏ, sửa lại làm xanh; xác nhận test nào thực sự đã chạy. |
| CD | Đưa artifact đã kiểm tra tới môi trường chạy bằng quy trình lặp lại. | Deploy thử trước, rồi tự động hóa; ban đầu xác nhận thủ công trước production. |
| Log / health / monitoring | Log kể chuyện lỗi; health báo khả năng phục vụ; theo dõi giúp biết lỗi khi không mở terminal. | Từ request thất bại tìm log tương ứng, không chỉ nhìn một trang Swagger 200. |
| Backup / restore / rollback | Backup và restore bảo vệ dữ liệu; rollback ứng dụng quay về phiên bản code. Hai việc không thay thế nhau. | Khôi phục bản sao DB thử và chạy lại bản app cũ tương thích. |

## 4. Thứ tự đề xuất và điều kiện qua mốc

Mỗi mốc chỉ bắt đầu bài mới sau khi chấm câu trả lời đang chờ. Có thể chia nhiều buổi; không giao đồng thời cả pipeline và toàn bộ frontend.

### M0 — Khép lại phần đang học, tạo baseline có thể chạy

- Trở lại câu hỏi đang dở trong CURRENT_STEP khi người học quay về code.
- Làm rõ nghỉ việc/hoạt động lại, ngày hiệu lực, lịch sử phân công và quyền ADMIN trước khi viết nốt service/controller.
- Đặc biệt: danh sách hiện tại lọc phân công còn hiệu lực, nên chỉ thêm `isActive=false` không đủ để tìm nhân viên đã nghỉ.
- Chốt quy tắc token đang còn hạn sau khi tài khoản bị vô hiệu hóa; JWT không tự truy vấn lại `AppUser.IsActive` sau mỗi request chỉ vì đã ký token.
- Hoàn thiện từng method, kiểm tra đồng thời thay đổi AppUser/EmployeeProfile/UserBranch và một lần lưu thống nhất. Không chèn stub để đạt build xanh.
- Nếu người học muốn chuyển sang frontend ngay, thống nhất cách tạm dừng phần chưa hoàn thành; không tự xóa thay đổi của họ.

Qua mốc khi build được, test các nhánh quyền/dữ liệu/ngày trên local, người học giải thích được flow. Commit checkpoint sạch khi người học yêu cầu.

### M1 — CI nhỏ và một luồng frontend chạy thật

Làm lần lượt hai đầu việc, không đợi xong tất cả module:

1. Học CI đầu tiên: checkout, chọn SDK tương thích, restore và build Release trên GitHub Actions. Dùng repo hiện có; chưa cần deploy tự động.
2. Thêm một nhóm test nhỏ cho logic dễ sai: quyền OWNER/ADMIN, phạm vi chi nhánh hoặc ngày hiệu lực. Phân biệt test service với integration test API/SQL; mở rộng dần, không đặt mục tiêu coverage tùy ý.
3. Frontend đầu tiên: đăng nhập -> xem tên/role -> chọn chi nhánh được phép -> danh sách và chi tiết nhân viên -> đăng xuất. Tận dụng API đã có.
4. Giải thích từng chặng `Components/Pages` -> lớp gọi API ở Web (sẽ tạo) -> Controller -> Service -> Repository -> DbContext -> SQL. HTTP đi qua JWT middleware trước khi action được chạy.
5. Chốt cách giữ trạng thái đăng nhập phù hợp Interactive Server; đối chiếu cách LocalStorage đã học và rủi ro XSS, cookie/server state trước khi chọn. Không tự áp dụng cơ chế của classic Blazor Server.
6. Có loading/empty/error/401/403, F5 và tài khoản ở hai trình duyệt không lẫn token/dữ liệu. Ẩn nút theo role không thay thế phân quyền backend.

Qua mốc khi người học thao tác được luồng trên UI, biết API nào được gọi và CI kiểm tra được code. CI xanh khi chưa có test chỉ chứng minh build; không gọi đó là “test đầy đủ”.

### M2 — Deploy thử sớm một vertical slice

Mục tiêu: phát hiện lỗi môi trường khi sản phẩm còn nhỏ. Có thể thử API + DB từ xa trước trong lúc đang nối UI; mốc hoàn tất vẫn phải có UI -> API -> DB chạy xuyên suốt.

- Chốt sơ đồ nơi chạy Web, API, SQL; chọn vùng, runtime, ngân sách và giới hạn trước khi tạo tài nguyên.
- Giữ DB Docker local; tạo database thử riêng trên cloud. Không dùng cloud production làm DB mặc định lúc lập trình.
- Publish Release và chạy thử với cấu hình Production ngoài IDE trước. Kiểm tra port, HTTPS, API base URL, connection string, JWT issuer/audience/key và tài liệu XML nếu cần Swagger.
- Triển khai thủ công có ghi các bước để hiểu: tạo nơi chạy -> cấu hình -> đưa artifact lên -> xem startup log -> smoke test. Không thuê VPS chỉ vì nghĩ deploy bắt buộc có VPS.
- Chuẩn bị schema và seed giả lập riêng cho DB đích; kiểm tra tính tương thích SQL Server/Azure SQL. Không chạy nguyên script tạo database local lên cloud mà chưa đọc.
- Kiểm tra đường đi từ Web server tới API, API tới database; `localhost` ở cloud/container không phải máy cá nhân.
- Với Interactive Server, kiểm tra render mode, kết nối Blazor, WebSockets, reconnect và session affinity theo nền tảng chọn. Không mặc định cần mua Azure SignalR Service. [Tài liệu Blazor hosting](https://learn.microsoft.com/en-us/aspnet/core/blazor/host-and-deploy/server/?view=aspnetcore-10.0).
- CORS chỉ liên quan khi trình duyệt gọi API khác origin; HttpClient chạy trên Web server không tự có vấn đề CORS. Phải vẽ đúng chỗ request xuất phát trước khi cấu hình.

Qua mốc khi tắt app local mà luồng public vẫn chạy từ một máy/trình duyệt khác; ghi rõ URL, phiên bản deploy, test pass/fail và giới hạn. Đây là deploy thử, chưa tuyên bố production hoàn chỉnh.

### M3 — CD cơ bản và vận hành bản public đầu tiên

- Từ cách deploy thủ công đã hiểu, tạo pipeline build/test/publish -> artifact có phiên bản -> deploy thử -> smoke test -> xác nhận phát hành production.
- Ưu tiên cơ chế xác thực CI tới cloud hạn chế quyền và không chứa secret dài hạn nếu nền tảng hỗ trợ; giải thích trước khi thêm OIDC. Không tự mua gói để có deployment slot/approval environment.
- Nếu ngân sách không đủ staging thường trực, dùng môi trường thử tạm và kiểm tra bản phát hành riêng trước khi đưa lên public. Không lấy DB production làm nơi chạy test ghi/xóa.
- Giữ bản artifact trước; thực hành một lần deploy lỗi và quay về bản tốt. Đối chiếu schema tương thích trước khi rollback app.
- Thêm xử lý lỗi production trả thông báo an toàn, log có request/trace identifier; không log password, token, connection string hoặc nguyên body hồ sơ nhân viên.
- Có health check, cách xem logs và kiểm tra tính sẵn sàng; tránh lộ hạ tầng/secret trong endpoint health public. Thiết lập theo dõi uptime và chi phí ở mức cần thiết.
- Kiểm tra bảo vệ login (giới hạn thử sai phù hợp), role/branch, token hết hạn hoặc tài khoản bị khóa, reset demo, dữ liệu nhạy cảm, Swagger public có chủ đích và secrets đã từng lộ phải được thay trước production.
- Database First: lưu script thay đổi tăng dần, có thứ tự/phiên bản; kiểm tra trên bản sao, backup trước thay đổi rủi ro. Scaffold cập nhật model trong lúc phát triển, không chạy scaffold/seed reset mỗi khi app production khởi động.
- Tập restore backup vào database thử, kiểm tra dữ liệu và app đọc được; backup có file nhưng chưa restore thử chưa đủ chứng minh phục hồi.

Qua mốc khi một vertical slice có thể phát hành lại, tìm lỗi, phục hồi và không lẫn dữ liệu local/public. Chưa cần scale nhiều instance hay hạ tầng enterprise.

### M4 — Mở rộng MVP bằng các vertical slice tiếp theo

Làm từng nhóm BE + FE + test + cập nhật bản public; không quay về cách viết tất cả API rồi mới ghép UI:

1. Hoàn thiện UI thao tác nhân viên đã có API: tạo, sửa, chuyển chi nhánh, nghỉ/hoạt động lại; quyền OWNER chỉ đọc, ADMIN ghi đúng phạm vi.
2. Menu và luồng tạo/xem order: xác định giá từ backend, dữ liệu lịch sử và lưu nhiều entity thống nhất; không tự thêm trừ nguyên liệu theo order vì nằm ngoài MVP.
3. Kho/kiểm kho theo nghiệp vụ đã chốt; kiểm tra cập nhật đồng thời để tránh sai số tồn.
4. Payroll sau khi chốt công thức lương; không tự đoán ngày công/cách tính. Quyền truy cập BaseSalary và lịch sử phải được kiểm tra.
5. Dashboard dùng dữ liệu thật; audit được thêm cùng các nghiệp vụ ghi cần lưu lịch sử, không để cuối cùng mới cố khôi phục hoạt động đã bỏ lỡ.

Danh mục [API v0.1](../context/api-endpoint-roadmap-v0.1.md) chỉ giúp rà soát scope. Từng module cần tiêu chí riêng. Nếu thời gian không đủ, cùng người học chốt phạm vi phiên bản portfolio; không lặng lẽ cắt yêu cầu đồ án.

### M5 — Chốt portfolio và khả năng giải thích khi phỏng vấn

- Luồng chính chạy bằng dữ liệu giả lập, responsive, lỗi hiển thị có ích. Chức năng chưa làm được ghi rõ, không trình bày số liệu seed là kết quả kinh doanh thật.
- README gồm mục tiêu, stack thực dùng, sơ đồ triển khai, flow một chức năng, cách chạy local không kèm secret, test, demo và giới hạn.
- Có lịch sử Git hợp lý, CI/CD đã chạy thật, release rõ phiên bản, checklist smoke test và hướng dẫn tìm lỗi/khôi phục.
- Đo hiệu năng với điều kiện cụ thể khi cần; sửa N+1, truy vấn dư, phân trang, index hay concurrency dựa vào bằng chứng, không thêm cache vì “production phải có”.
- Người học tự trình bày một luồng đọc, một luồng ghi nhiều bảng, một lỗi production đã tìm ra và một lần phát hành. CV chỉ ghi tính năng/công nghệ/kết quả đã làm và hiểu.

## 5. Hướng hosting đang cân nhắc, chưa phải quyết định mua/tạo

Sơ đồ mục tiêu đơn giản: Browser -> Blazor Web host -> ASP.NET Core API host -> SQL database. Application và Infrastructure là thư viện được dùng cùng ứng dụng, không cần server riêng cho mỗi class library.

- Ưu tiên đánh giá Azure vì người học đã có tài khoản Education. Có thể dùng App Service cho app và Azure SQL cho DB nếu runtime, kết nối Interactive Server, vùng và chi phí phù hợp. Nếu runtime không phù hợp mới so sánh container; không mặc định container rẻ hơn.
- Chưa cần mua domain; dùng hostname/HTTPS do nền tảng cung cấp nếu gói chọn có hỗ trợ. PaaS có thể tránh phải quản trị VPS, nhưng vẫn phải cấu hình và theo dõi app.
- Kiểm tra offer áp dụng thật trên subscription trước khi tạo. Azure SQL free offer có hạn mức, lựa chọn khi chạm hạn mức và hạn chế import/restore; không cam kết di chuyển DB cũ chỉ bằng đổi connection string. [Azure SQL free offer](https://learn.microsoft.com/en-us/azure/azure-sql/database/free-offer?view=azuresql).
- Đặt budget/alerts và ghi cách dừng/xóa tài nguyên không dùng; budget alert không tự dừng tài nguyên hoặc chi tiêu. [Microsoft Cost Management](https://learn.microsoft.com/en-us/azure/cost-management-billing/costs/tutorial-acm-create-budgets).
- Chốt mức chi trả chấp nhận được, quota/credit còn lại, DB demo có cần giữ nguyên và thời gian cần online. Không coi ảnh số credit trước đây là số dư hiện tại.

## 6. Việc cần học sớm và việc để sau

Cần sớm: config khác môi trường, auth/branch scope, frontend gọi API, test rủi ro cao, Git/CI, publish, logs, backup, chi phí và deploy một luồng nhỏ.

Cần trước public ổn định: phục hồi, secrets thật, giới hạn login, xử lý lỗi an toàn, dữ liệu demo, kiểm tra quyền bằng hai tài khoản, kết nối lại của Blazor, quy trình phát hành và thay đổi schema.

Để sau khi có nhu cầu/bằng chứng: Kubernetes, microservices, API Gateway, service mesh, Redis cache, event bus, multi-region, autoscaling phức tạp, IaC toàn bộ. Không xem đây là điều kiện bắt buộc đi intern.

## 7. Hợp đồng hướng dẫn trong các buổi sau

Mỗi buổi: chấm câu trả lời trước -> nhắc đúng điểm dừng -> giải thích phần nhỏ tiếp theo -> người học thực hành -> kiểm tra bằng chứng -> cập nhật checkpoint. Lỗi build, nghiệp vụ, mạng, cấu hình và database phải được phân biệt, không suy đoán từ chữ “500”.

Với “check”, đọc code mới nhất và nhận xét; với “hướng dẫn”, không tự hoàn thiện thay người học; với “tự sửa”, chỉ sửa phạm vi đã yêu cầu. Mỗi đề xuất mới nói rõ phục vụ mốc nào. Đọc code không chứng minh runtime; pipeline chưa chạy không chứng minh CI/CD xong. [GitHub: build và test .NET](https://docs.github.com/en/actions/tutorials/build-and-test-code/net).

Không lưu token/password/dữ liệu nhân viên thật trong checkpoint. Ghi quyết định lâu dài ở PROJECT_CONTEXT; câu hỏi và bài đang dở ở CURRENT_STEP. Các mốc ở trên chưa được đánh dấu hoàn thành nếu chưa có bằng chứng.
