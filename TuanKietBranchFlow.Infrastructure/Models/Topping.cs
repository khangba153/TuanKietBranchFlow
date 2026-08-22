using System;
using System.Collections.Generic;

namespace TuanKietBranchFlow.Infrastructure.Models;

public partial class Topping
{
    public int Id { get; set; }

    public int ToppingGroupId { get; set; }

    public string Name { get; set; } = null!;

    public decimal Price { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public bool Deleted { get; set; }

    public virtual ICollection<BranchTopping> BranchToppings { get; set; } = new List<BranchTopping>();

    public virtual ICollection<OrderItemTopping> OrderItemToppings { get; set; } = new List<OrderItemTopping>();

    public virtual ToppingGroup ToppingGroup { get; set; } = null!;
}
