namespace TuanKietBranchFlow.Application.DTOs.MenuManagement;

public class CreateToppingGroupResultDTO
{
    public bool IsNameInvalid { get; set; }
    public bool IsNameDuplicated { get; set; }
    public MenuToppingGroupDTO? ToppingGroup { get; set; }
}