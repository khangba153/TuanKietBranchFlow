using System;
using System.Collections.Generic;

namespace TuanKietBranchFlow.Infrastructure.Models;

public partial class Stocktake
{
    public int Id { get; set; }

    public string Code { get; set; } = null!;

    public int BranchId { get; set; }

    public int CheckedByUserId { get; set; }

    public int? AdjustmentTransactionId { get; set; }

    public DateTime CompletedAt { get; set; }

    public virtual StockTransaction? AdjustmentTransaction { get; set; }

    public virtual Branch Branch { get; set; } = null!;

    public virtual AppUser CheckedByUser { get; set; } = null!;

    public virtual ICollection<StocktakeItem> StocktakeItems { get; set; } = new List<StocktakeItem>();
}
