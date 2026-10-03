using TuanKietBranchFlow.Application.DTOs.MenuManagement;

namespace TuanKietBranchFlow.Application.Services;

public interface INoteOptionService
{
    // Tạo lựa chọn ghi chú trong nhóm đã chọn và trả kết quả nghiệp vụ
    Task<CreateNoteOptionResultDTO> CreateNoteOptionAsync(
        CreateNoteOptionRequestDTO request);

    // Cập nhật tên và trạng thái lựa chọn ghi chú trong nhóm hiện tại
    Task<UpdateNoteOptionResultDTO> UpdateNoteOptionAsync(
        int noteOptionId,
        UpdateNoteOptionRequestDTO request);

    // Xóa mềm lựa chọn ghi chú, giữ dữ liệu liên quan và lịch sử đơn hàng
    Task<DeleteNoteOptionResultDTO> DeleteNoteOptionAsync(int noteOptionId);
}
