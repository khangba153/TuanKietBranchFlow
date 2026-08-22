using System;
using System.Collections.Generic;

namespace TuanKietBranchFlow.Infrastructure.Models;

public partial class ProductSize
{
    public int Id { get; set; }

    public int ProductId { get; set; }

    public int SizeId { get; set; }

    public decimal Price { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public bool Deleted { get; set; }

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public virtual Product Product { get; set; } = null!;

    public virtual Size Size { get; set; } = null!;
}
