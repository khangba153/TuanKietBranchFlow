namespace TuanKietBranchFlow.Application.DTOs.OrderMenus;

public class OrderMenuNoteGroupDTO
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    // Mỗi nhóm ghi chú chỉ được chọn 1 phương án
    public int MaxSelections { get; set; } = 1;

    public List<OrderMenuNoteOptionDTO> Options { get; set; }
        = new List<OrderMenuNoteOptionDTO>();
}