using System.ComponentModel.DataAnnotations;

namespace TuanKietBranchFlow.Application.DTOs.Orders.Management;

public class OrderManagementQueryDTO
{
    [Range(1, int.MaxValue)]
    public int? BranchId { get; set; }
    public DateOnly? BusinessDate { get; set; }
    [StringLength(100)]
    public string? Keyword { get; set; }
    [StringLength(30)]
    public string? Status { get; set; }
    [Range(1, 1000000)]
    public int Page { get; set; } = 1;
    [Range(1, 100)]
    public int PageSize { get; set; } = 20;
}
