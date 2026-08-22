using System;
using System.Collections.Generic;

namespace TuanKietBranchFlow.Infrastructure.Models;

public partial class ToppingGroup
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public bool Deleted { get; set; }

    public virtual ICollection<Topping> Toppings { get; set; } = new List<Topping>();
}
