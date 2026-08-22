using System;
using System.Collections.Generic;

namespace TuanKietBranchFlow.Infrastructure.Models;

public partial class BranchIngredient
{
    public int Id { get; set; }

    public int BranchId { get; set; }

    public int IngredientId { get; set; }

    public decimal Quantity { get; set; }

    public decimal WarningThreshold { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public byte[] RowVersion { get; set; } = null!;

    public virtual Branch Branch { get; set; } = null!;

    public virtual Ingredient Ingredient { get; set; } = null!;
}
