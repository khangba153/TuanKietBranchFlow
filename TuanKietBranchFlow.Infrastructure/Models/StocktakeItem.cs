using System;
using System.Collections.Generic;

namespace TuanKietBranchFlow.Infrastructure.Models;

public partial class StocktakeItem
{
    public int Id { get; set; }

    public int StocktakeId { get; set; }

    public int IngredientId { get; set; }

    public decimal SystemQuantity { get; set; }

    public decimal ActualQuantity { get; set; }

    public decimal? Difference { get; set; }

    public virtual Ingredient Ingredient { get; set; } = null!;

    public virtual Stocktake Stocktake { get; set; } = null!;
}
