using System.Text.Json;
using TuanKietBranchFlow.Application.DTOs.Orders.Management;

namespace TuanKietBranchFlow.Application.Services.Orders;
// Snapshot không lồng history cũ để tránh JSON tăng theo từng lần xử lý.
public static class OrderSnapshot
{
    public static string Serialize(OrderManagementDetailDTO detail) => JsonSerializer.Serialize(new { detail.Id, detail.Code, detail.BranchId, detail.CreatedByUserId, detail.BusinessDate, detail.CreatedAt, detail.Status, detail.TotalAmount, detail.ReportReason, detail.ReportedAt, detail.ReportedByUserId, detail.ReportedByFullName, detail.Items });
}
