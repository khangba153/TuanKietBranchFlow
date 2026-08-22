using System;
using System.Collections.Generic;

namespace TuanKietBranchFlow.Infrastructure.Models;

public partial class Product
{
    public int Id { get; set; }

    public int CategoryId { get; set; }

    public string Name { get; set; } = null!;

    public string? ImageUrl { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public bool Deleted { get; set; }

    public virtual ICollection<BranchProduct> BranchProducts { get; set; } = new List<BranchProduct>();

    public virtual Category Category { get; set; } = null!;

    public virtual ICollection<ProductSize> ProductSizes { get; set; } = new List<ProductSize>();
}
