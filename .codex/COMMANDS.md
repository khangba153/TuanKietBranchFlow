# Cách tiếp tục làm việc với BranchFlow

Mở task tại `D:\DU AN\TuanKietBranchFlow` để dùng quy tắc và skill của repo này. Mở task ở BE_MARKET không tự biến BranchFlow thành project đang được nạp.

## Nguồn ngữ cảnh

- `AGENTS.md`: cách hướng dẫn và đường dẫn đến quy tắc từng tầng.
- `PROJECT_CONTEXT.md`: nghiệp vụ và quyết định lâu dài.
- `docs/learning/CURRENT_STEP.md`: câu trả lời đã đạt, câu hỏi đang chờ và bài đang dở.
- `docs/learning/production-learning-roadmap.md`: mốc học–frontend–CI–deploy–vận hành.

Các file `AGENTS-API.md`, `AGENTS-APPLICATION.md`, `AGENTS-INFRASTRUCTURE.md`, `AGENTS-WEB.md` được root AGENTS.md yêu cầu đọc theo phạm vi, không giả định tên tùy chỉnh này được tự nạp. Cơ chế tự tìm instruction file được mô tả trong [tài liệu AGENTS.md](https://learn.chatgpt.com/docs/agent-configuration/agents-md).

Skill nằm ở `.agents/skills`. Các skill cũ giữ tên `$marketplace-*` để không làm hỏng cách gọi quen thuộc, nhưng nội dung và build script trong repo này đã được chỉnh cho BranchFlow. Không dùng nhầm bản skill cùng tên từ BE_MARKET. [Tài liệu skill](https://learn.chatgpt.com/docs/build-skills).

| Skill | Khi dùng |
| --- | --- |
| `$branchflow-guided-learning` | Chấm câu trả lời trước, đọc checkpoint, hướng dẫn đúng một bước; đối chiếu nguồn thầy khi cần. |
| `$marketplace-review` | Review có bằng chứng, không tự sửa. |
| `$marketplace-build` | Build bốn project BranchFlow và phân biệt compile với runtime. |
| `$marketplace-architecture` | Thực hiện thay đổi backend được yêu cầu, giữ flow và phạm vi bài học. |
| `$marketplace-blazor-bootstrap` | Làm UI .NET 10 Blazor Web App Interactive Server với Bootstrap. |

## Lời nhắc để bắt đầu buổi mới

“Đọc AGENTS.md, PROJECT_CONTEXT.md và docs/learning/CURRENT_STEP.md. Nhắc lại đúng điểm đang dở, kiểm tra câu trả lời của tôi trước, rồi hướng dẫn một bước tiếp theo theo lộ trình học–production.”

Lưu các file quy tắc/context cùng Git khi người học yêu cầu commit. Không lưu secrets hoặc chép toàn bộ chat. Các file giúp nối lại ngữ cảnh, không bảo đảm agent nhớ toàn bộ hội thoại hoặc luôn biết thay đổi chưa được ghi nhận.

Các agent trong `.codex/agents` chỉ phục vụ công việc được giao rõ ràng; không dùng để tự viết toàn bộ bài học. Không có cấu hình nào trong đây tự tạo cloud resource hoặc triển khai ứng dụng.
