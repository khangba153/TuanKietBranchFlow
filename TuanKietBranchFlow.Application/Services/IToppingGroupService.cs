using TuanKietBranchFlow.Application.DTOs.MenuManagement;

namespace TuanKietBranchFlow.Application.Services;

public interface IToppingGroupService
{
    // Lấy các nhóm topping và topping để ADMIN quản lý, OWNER xem
    Task<List<MenuToppingGroupDTO>> GetAllToppingGroupsAsync();

    // Tạo nhóm topping mới
    Task<CreateToppingGroupResultDTO> CreateToppingGroupAsync(
        CreateToppingGroupRequestDTO request);

    // Cập nhật tên và trạng thái của nhóm topping
    Task<UpdateToppingGroupResultDTO> UpdateToppingGroupAsync(
        int toppingGroupId,
        UpdateToppingGroupRequestDTO request);

    // Xóa mềm nhóm topping, giữ nguyên dữ liệu topping con
    Task<DeleteToppingGroupResultDTO> DeleteToppingGroupAsync(
        int toppingGroupId);
}