using System.ComponentModel.DataAnnotations;

namespace TuanKietBranchFlow.Application.DTOs.Orders.Management;

public class AdjustOrderRequestDTO
{
    [Required, StringLength(500, MinimumLength = 1)]
    public string Reason { get; set; } = "";

    [Required]
    public DateTime? ExpectedReportedAt { get; set; }

    [Range(0, int.MaxValue)]
    public int ExpectedHistoryId { get; set; }

    [Required, MinLength(1), MaxLength(100)]
    public List<AdjustOrderItemRequestDTO> Items { get; set; } = new List<AdjustOrderItemRequestDTO>();

    [StringLength(64)]
    public string? PreviewHash { get; set; }
}
