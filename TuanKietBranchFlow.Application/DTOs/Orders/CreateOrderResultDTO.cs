namespace TuanKietBranchFlow.Application.DTOs.Orders;

public class CreateOrderResultDTO
{
    // Chi nhánh phải tồn tại và chưa bị xóa
    public bool IsBranchFound { get; set; }

    // Employee phải có phân công còn hiệu lực tại chi nhánh
    public bool HasAccess { get; set; }

    // Request phải dùng các món và lựa chọn hợp lệ trong db
    public bool IsOrderValid { get; set; }

    // Giải thích lỗi nghiệp vụ để Controller trả cho web
    public string? ErrorMessage { get; set; }

    // Chỉ có dữ liệu khi đơn được tạo thành công
    public CreateOrderResponseDTO? Order { get; set; }
}