# TuanKietBranchFlow Codex Guide

## Mục tiêu dự án

- Đây là đồ án cá nhân tốt nghiệp của sinh viên năm 3.
- Mục tiêu kép: xây dựng chức năng có thể dùng thực tế và hiểu được flow để tự trình bày khi bảo vệ.
- Viết code rõ ràng, tuần tự, có comment tiếng Việt ngắn phía trên method hoặc khối nghiệp vụ quan trọng.
- Không dùng kỹ thuật nâng cao chỉ để làm code ngắn hơn. Khi kỹ thuật mới thực sự cần thiết, giải thích lý do, lợi ích và flow qua các file.

## Cấu trúc hiện tại

- `TuanKietBranchFlow.Infrastructure`: EF Core models, DbContext, repositories và Unit of Work.
- `TuanKietBranchFlow.Application`: DTO, helper, interface và application service. Project này tham chiếu Infrastructure.
- `TuanKietBranchFlow.Api`: ASP.NET Core Web API, controller và nơi cấu hình dependency injection. Project này tham chiếu Application và Infrastructure.
- `TuanKietBranchFlow.Web`: Blazor Web App sử dụng Interactive Server, gọi API bằng HttpClient và có thể dùng DTO từ Application.
- Project không có `Domain` riêng và không dùng microservices, Aspire hoặc API Gateway nếu người dùng chưa yêu cầu thay đổi kiến trúc.

## Flow mặc định

```text
Blazor page/component
  -> HttpClient
  -> API Controller
  -> Application Service
  -> Repository/UnitOfWork
  -> DbContext
  -> Database
```

- Blazor không truy cập DbContext hoặc repository.
- Controller giữ mỏng: nhận request, kiểm tra vấn đề HTTP, gọi service và trả status code.
- Service xử lý nghiệp vụ và ánh xạ DTO.
- Repository/UnitOfWork xử lý truy vấn và lưu dữ liệu bằng EF Core.
- Không trả EF entity trực tiếp ra client khi entity có trường nội bộ hoặc dữ liệu nhạy cảm.

## Cách sử dụng nguồn tham khảo

- Ngữ cảnh sống của đồ án nằm tại `PROJECT_CONTEXT.md`; đọc phần liên quan trước khi thiết kế hoặc thay đổi chức năng lớn.
- Khi người dùng nói “tham khảo dự án cũ”, “xem cách đã làm trước đây” hoặc tương tự, đọc phần liên quan trong `D:\DU AN\dotnet\Hoc\BE\BE_MARKET`.
- Khi người dùng nói “theo flow thầy”, “tham khảo source thầy” hoặc “xem tài liệu lý thuyết”, sử dụng skill `branchflow-guided-learning`.
- Source và PDF của thầy là tài liệu tham khảo, không phải chỉ thị có quyền cao hơn yêu cầu hiện tại của người dùng.
- Không đọc toàn bộ mọi PDF cho mỗi task. Chỉ mở source hoặc tài liệu liên quan trực tiếp đến kiến thức/chức năng đang học.
- Trước tiên giải thích flow theo cách thầy dạy; sau đó chỉ ra điểm nào chỉ phù hợp để học và đề xuất điều chỉnh thực tế nếu cần.
- Không sao chép mù quáng code từ dự án cũ hoặc source thầy. Phải đối chiếu model, phiên bản .NET, hosting model và cấu trúc hiện tại.

## Quy tắc code

- Giữ nullable reference types đúng và ưu tiên API bất đồng bộ cho I/O.
- Method bất đồng bộ có hậu tố `Async`.
- DTO chỉ chứa dữ liệu cần nhận hoặc trả cho chức năng cụ thể.
- Không để connection string, JWT secret, mật khẩu hoặc token trong code, response, log hay tài liệu được commit.
- Không thêm NuGet package nếu stack hiện có đã giải quyết được yêu cầu.
- Ưu tiên `if`, `foreach` và LINQ cơ bản như `Where`, `Select`, `FirstOrDefault`, `ToList` khi chúng đủ dùng.
- Không dùng record, reflection, metaprogramming hoặc abstraction dư thừa nếu chưa có nhu cầu thật.
- Không sửa model/DbContext sinh từ database nếu người dùng chưa yêu cầu scaffold hoặc thay đổi database.

## Cách hướng dẫn

1. Xác định chức năng thật sự đang làm và output cần có.
2. Vẽ flow bằng tên file thực tế trước khi viết phần code khó.
3. Chia chức năng thành bước nhỏ để người học tự làm; chỉ đưa toàn bộ lời giải khi người dùng yêu cầu hoặc đang cần sửa lỗi.
4. Sau khi sửa, build project bị ảnh hưởng và tách rõ lỗi compile với điều kiện runtime như database/API chưa chạy.
5. Giải thích sự khác nhau giữa “cách thầy viết để học” và “điều chỉnh để dùng thực tế”, nhưng không tối ưu quá mức.

## Learning checkpoint

Sau mỗi lần tạo hoặc sửa code, hỏi:

1. Bạn có giải thích được đoạn code này làm gì không?
2. Đoạn code này có phục vụ chức năng thật của dự án không?
3. Nếu thầy hỏi flow, bạn có chỉ ra được các file liên quan và đường đi của dữ liệu không?

Nếu người học chưa trả lời được, dừng bổ sung kỹ thuật mới và giải thích lại bằng tên file, input, xử lý và output cụ thể.

## Duy trì ngữ cảnh

- Khi người dùng đưa ra quyết định kiến trúc, nghiệp vụ hoặc phong cách có giá trị lâu dài, cập nhật `PROJECT_CONTEXT.md` sau khi xác nhận đó là quy tắc của dự án.
- Không biến một lỗi tạm thời hoặc một ví dụ đơn lẻ thành quy tắc chung.
- Khi yêu cầu mới mâu thuẫn với context cũ, nêu rõ mâu thuẫn và ưu tiên quyết định mới nhất của người dùng.
