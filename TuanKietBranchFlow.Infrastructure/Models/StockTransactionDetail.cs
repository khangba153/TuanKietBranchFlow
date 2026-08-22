using System;
using System.Collections.Generic;

namespace TuanKietBranchFlow.Infrastructure.Models;

public partial class StockTransactionDetail
{
    public int Id { get; set; }

    public int StockTransactionId { get; set; }

    public int IngredientId { get; set; }

    public decimal QuantityChange { get; set; }

    public decimal QuantityBefore { get; set; }

    public decimal QuantityAfter { get; set; }

    public virtual Ingredient Ingredient { get; set; } = null!;

    public virtual StockTransaction StockTransaction { get; set; } = null!;
}
