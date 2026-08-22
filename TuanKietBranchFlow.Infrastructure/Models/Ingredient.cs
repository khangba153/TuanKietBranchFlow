using System;
using System.Collections.Generic;

namespace TuanKietBranchFlow.Infrastructure.Models;

public partial class Ingredient
{
    public int Id { get; set; }

    public int UnitId { get; set; }

    public string Name { get; set; } = null!;

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public bool Deleted { get; set; }

    public virtual ICollection<BranchIngredient> BranchIngredients { get; set; } = new List<BranchIngredient>();

    public virtual ICollection<StockTransactionDetail> StockTransactionDetails { get; set; } = new List<StockTransactionDetail>();

    public virtual ICollection<StocktakeItem> StocktakeItems { get; set; } = new List<StocktakeItem>();

    public virtual IngredientUnit Unit { get; set; } = null!;
}
