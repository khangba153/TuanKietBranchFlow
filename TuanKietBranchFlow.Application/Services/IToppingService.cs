using TuanKietBranchFlow.Application.DTOs.MenuManagement;

namespace TuanKietBranchFlow.Application.Services;

public interface IToppingService
{
    // Tạo topping trong nhóm đã chọn và trả kết quả nghiệp vụ
    Task<CreateToppingResultDTO> CreateToppingAsync(
        CreateToppingRequestDTO request);

    // Cập nhật tên, giá và trạng thái topping trong nhóm hiện tại
    Task<UpdateToppingResultDTO> UpdateToppingAsync(
        int toppingId,
        UpdateToppingRequestDTO request);

    // Xóa mềm topping, giữ dữ liệu liên quan và lịch sử đơn hàng
    Task<DeleteToppingResultDTO> DeleteToppingAsync(int toppingId);
}
