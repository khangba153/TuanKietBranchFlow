namespace TuanKietBranchFlow.Application.DTOs.MenuManagement;

public class CreateSizeResultDTO
{
    public bool IsNameInvalid { get; set; }
    public bool IsNameDuplicated { get; set; }
    public MenuSizeDTO? Size { get; set; }
}