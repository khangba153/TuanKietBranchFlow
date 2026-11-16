namespace TuanKietBranchFlow.Application.DTOs.Orders.Management;

public class OrderReviewResultDTO
{
    public bool IsForbidden { get; set; }
    public bool IsInvalid { get; set; }
    public bool IsNotFound { get; set; }
    public bool IsConflict { get; set; }
    public string? Status { get; set; }
}
