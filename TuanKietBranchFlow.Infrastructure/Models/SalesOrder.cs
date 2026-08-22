using System;
using System.Collections.Generic;

namespace TuanKietBranchFlow.Infrastructure.Models;

public partial class SalesOrder
{
    public int Id { get; set; }

    public string Code { get; set; } = null!;

    public int BranchId { get; set; }

    public int CreatedByUserId { get; set; }

    public DateOnly BusinessDate { get; set; }

    public int DailySequence { get; set; }

    public decimal TotalAmount { get; set; }

    public string Status { get; set; } = null!;

    public string? ReportReason { get; set; }

    public int? ReportedByUserId { get; set; }

    public DateTime? ReportedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Branch Branch { get; set; } = null!;

    public virtual AppUser CreatedByUser { get; set; } = null!;

    public virtual ICollection<OrderAdjustment> OrderAdjustments { get; set; } = new List<OrderAdjustment>();

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public virtual AppUser? ReportedByUser { get; set; }
}
