using System;
using System.Collections.Generic;

namespace TuanKietBranchFlow.Infrastructure.Models;

public partial class OrderAdjustment
{
    public int Id { get; set; }

    public int SalesOrderId { get; set; }

    public int AdjustedByUserId { get; set; }

    public string Reason { get; set; } = null!;

    public string BeforeData { get; set; } = null!;

    public string AfterData { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual AppUser AdjustedByUser { get; set; } = null!;

    public virtual SalesOrder SalesOrder { get; set; } = null!;
}
