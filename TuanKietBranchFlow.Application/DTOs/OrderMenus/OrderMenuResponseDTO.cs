namespace TuanKietBranchFlow.Application.DTOs.OrderMenus;

public class OrderMenuResponseDTO
{
    public OrderMenuBranchDTO Branch { get; set; } =
        new OrderMenuBranchDTO();

    public List<OrderMenuCategoryDTO> Categories { get; set; } =
        new List<OrderMenuCategoryDTO>();

    public List<OrderMenuToppingGroupDTO> ToppingGroups { get; set; } =
        new List<OrderMenuToppingGroupDTO>();

    public List<OrderMenuNoteGroupDTO> NoteGroups { get; set; } =
        new List<OrderMenuNoteGroupDTO>();
}