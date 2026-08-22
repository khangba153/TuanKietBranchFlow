using System;
using System.Collections.Generic;

namespace TuanKietBranchFlow.Infrastructure.Models;

public partial class BranchTopping
{
    public int Id { get; set; }

    public int BranchId { get; set; }

    public int ToppingId { get; set; }

    public bool IsAvailable { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Branch Branch { get; set; } = null!;

    public virtual Topping Topping { get; set; } = null!;
}
