using TuanKietBranchFlow.Application.DTOs.MenuManagement;

namespace TuanKietBranchFlow.Application.Services;

public interface ISizeService
{
    // Lấy danh sách size cho ADMIN quản lý và OWNER xem
    Task<List<MenuSizeDTO>> GetAllSizesAsync();

    // Tạo Size mới và trả kết quả kiểm tra cho Controller
    Task<CreateSizeResultDTO> CreateSizeAsync(
        CreateSizeRequestDTO request);

    // Cập nhật tên và trạng thái hoạt động của Size
    Task<UpdateSizeResultDTO> UpdateSizeAsync(
        int sizeId,
        UpdateSizeRequestDTO request);

    // Xóa mềm Size theo Id
    Task<DeleteSizeResultDTO> DeleteSizeAsync(int sizeId);
}
