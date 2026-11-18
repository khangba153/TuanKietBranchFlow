using System.ComponentModel.DataAnnotations;

namespace TuanKietBranchFlow.Application.DTOs.Orders.Management;

public class ResolveOrderReviewRequestDTO
{
    [Required, RegularExpression("^(KEEP|CANCEL)$")]
    public string Action { get; set; } = "";

    [Required, StringLength(500, MinimumLength = 1)]
    public string Reason { get; set; } = "";

    [Required]
    public DateTime? ExpectedReportedAt { get; set; }

    [Range(0, int.MaxValue)]
    public int ExpectedHistoryId { get; set; }
}
