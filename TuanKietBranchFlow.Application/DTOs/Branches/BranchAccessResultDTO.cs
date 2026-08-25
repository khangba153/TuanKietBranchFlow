namespace TuanKietBranchFlow.Application.DTOs.Branches;

public class BranchAccessResultDTO
{
    public bool IsFound { get; set; }
    public bool HasAccess { get; set; }
    public AccessibleBranchDTO? Branch { get; set; }
}