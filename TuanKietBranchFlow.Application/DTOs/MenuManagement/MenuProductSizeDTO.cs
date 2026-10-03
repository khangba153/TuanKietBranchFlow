namespace TuanKietBranchFlow.Application.DTOs.MenuManagement;

public class MenuProductSizeDTO
{
    // Id của dòng ProductSize
    public int Id { get; set; }

    public int SizeId { get; set; }
    public string SizeName { get; set; } = string.Empty;
    public decimal Price { get; set; }

    // Trạng thái của cặp Product–Size
    public bool IsProductSizeActive { get; set; }

    // Size chung còn được phép dùng trong menu hay không
    public bool IsSizeAvailable { get; set; }
}