using System;
using System.Collections.Generic;

namespace TuanKietBranchFlow.Infrastructure.Models;

public partial class OrderItemTopping
{
    public int Id { get; set; }

    public int OrderItemId { get; set; }

    public int ToppingId { get; set; }

    public string ToppingNameSnapshot { get; set; } = null!;

    public int Quantity { get; set; }

    public decimal UnitPriceSnapshot { get; set; }

    public virtual OrderItem OrderItem { get; set; } = null!;

    public virtual Topping Topping { get; set; } = null!;
}
