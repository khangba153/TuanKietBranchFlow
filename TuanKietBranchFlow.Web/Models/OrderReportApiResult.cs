using TuanKietBranchFlow.Application.DTOs.Orders;

namespace TuanKietBranchFlow.Web.Models;

public class OrderReportApiResult
{
    public bool IsSuccess { get; set; }
    public int StatusCode { get; set; }
    public ReportOrderResponseDTO? Order { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
}