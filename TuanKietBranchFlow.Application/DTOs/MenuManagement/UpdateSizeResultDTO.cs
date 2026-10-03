namespace TuanKietBranchFlow.Application.DTOs.MenuManagement;

public class UpdateSizeResultDTO
{
    public bool IsSizeNotFound { get; set; }
    public bool IsRequestInvalid { get; set; }
    public bool IsNameDuplicated { get; set; }
    public MenuSizeDTO? Size { get; set; }
}