namespace TuanKietBranchFlow.Application.DTOs.OrderMenus;

public class OrderMenuResultDTO
{
    // Cho biết chi nhánh có tồn tại và chưa bị xóa hay không
    public bool IsBranchFound { get; set; }

    // Cho biết employee có phân công còn hiệu lực tại chi nhánh
    public bool HasAccess { get; set; }

    // Chỉ có dữ liệu khi chi nhánh hợp lệ và employee có quyền
    public OrderMenuResponseDTO? Menu { get; set; }
}