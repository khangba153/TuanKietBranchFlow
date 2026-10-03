using TuanKietBranchFlow.Application.DTOs.MenuManagement;

namespace TuanKietBranchFlow.Application.Services;

public interface IProductService
{
    // Lấy danh sách món cho ADMIN quản lý và OWNER xem
    Task<List<MenuProductDTO>> GetAllProductsAsync();

    // Tạo món mới và trả kết quả kiểm tra cho Controller
    Task<CreateProductResultDTO> CreateProductAsync(
        CreateProductRequestDTO request);

    // Cập nhật thông tin Product và các cặp size–giá
    Task<UpdateProductResultDTO> UpdateProductAsync(
        int productId,
        UpdateProductRequestDTO request);

    // Xóa mềm Product
    Task<DeleteProductResultDTO> DeleteProductAsync(int productId);
}