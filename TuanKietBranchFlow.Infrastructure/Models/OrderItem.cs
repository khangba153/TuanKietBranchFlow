using System;
using System.Collections.Generic;

namespace TuanKietBranchFlow.Infrastructure.Models;

public partial class OrderItem
{
    public int Id { get; set; }

    public int SalesOrderId { get; set; }

    public int ProductSizeId { get; set; }

    public string ProductNameSnapshot { get; set; } = null!;

    public string SizeNameSnapshot { get; set; } = null!;

    public int Quantity { get; set; }

    public decimal UnitPriceSnapshot { get; set; }

    public decimal SubtotalAmount { get; set; }

    public virtual ICollection<OrderItemNote> OrderItemNotes { get; set; } = new List<OrderItemNote>();

    public virtual ICollection<OrderItemTopping> OrderItemToppings { get; set; } = new List<OrderItemTopping>();

    public virtual ProductSize ProductSize { get; set; } = null!;

    public virtual SalesOrder SalesOrder { get; set; } = null!;
}
