using System;
using System.Collections.Generic;

namespace TuanKietBranchFlow.Infrastructure.Models;

public partial class StockTransaction
{
    public int Id { get; set; }

    public int BranchId { get; set; }

    public int PerformedByUserId { get; set; }

    public string Type { get; set; } = null!;

    public int? OriginalTransactionId { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Branch Branch { get; set; } = null!;

    public virtual StockTransaction? InverseOriginalTransaction { get; set; }

    public virtual StockTransaction? OriginalTransaction { get; set; }

    public virtual AppUser PerformedByUser { get; set; } = null!;

    public virtual ICollection<StockTransactionDetail> StockTransactionDetails { get; set; } = new List<StockTransactionDetail>();

    public virtual Stocktake? Stocktake { get; set; }
}
