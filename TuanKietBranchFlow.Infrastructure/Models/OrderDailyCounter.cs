using System;
using System.Collections.Generic;

namespace TuanKietBranchFlow.Infrastructure.Models;

public partial class OrderDailyCounter
{
    public int BranchId { get; set; }

    public DateOnly BusinessDate { get; set; }

    public int LastNumber { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual Branch Branch { get; set; } = null!;
}
