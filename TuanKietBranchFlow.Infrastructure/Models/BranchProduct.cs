using System;
using System.Collections.Generic;

namespace TuanKietBranchFlow.Infrastructure.Models;

public partial class BranchProduct
{
    public int Id { get; set; }

    public int BranchId { get; set; }

    public int ProductId { get; set; }

    public bool IsAvailable { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Branch Branch { get; set; } = null!;

    public virtual Product Product { get; set; } = null!;
}
