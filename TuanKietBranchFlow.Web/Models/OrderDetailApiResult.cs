using TuanKietBranchFlow.Application.DTOs.Orders;

namespace TuanKietBranchFlow.Web.Models;

public class OrderDetailApiResult
{
    public bool IsSuccess { get; set; }
    public int StatusCode { get; set; }
    public MyOrderDetailDTO? Order { get; set; }
    public string? ErrorMessage { get; set; }
}