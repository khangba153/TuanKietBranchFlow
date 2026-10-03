using System.ComponentModel.DataAnnotations;

namespace TuanKietBranchFlow.Application.DTOs.MenuManagement;

public class CreateProductSizeRequestDTO
{
    [Range(1, int.MaxValue, ErrorMessage = "Size không hợp lệ.")]
    public int SizeId { get; set; }

    [Range(typeof(decimal), "0.01", "9999999999999999.99",
        ErrorMessage = "Giá phải lớn hơn 0.")]
    public decimal Price { get; set; }
}