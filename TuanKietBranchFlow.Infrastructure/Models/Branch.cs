using System;
using System.Collections.Generic;

namespace TuanKietBranchFlow.Infrastructure.Models;

public partial class Branch
{
    public int Id { get; set; }

    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string Address { get; set; } = null!;

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public bool Deleted { get; set; }

    public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();

    public virtual ICollection<BranchIngredient> BranchIngredients { get; set; } = new List<BranchIngredient>();

    public virtual ICollection<BranchProduct> BranchProducts { get; set; } = new List<BranchProduct>();

    public virtual ICollection<BranchTopping> BranchToppings { get; set; } = new List<BranchTopping>();

    public virtual ICollection<OrderDailyCounter> OrderDailyCounters { get; set; } = new List<OrderDailyCounter>();

    public virtual ICollection<Payroll> Payrolls { get; set; } = new List<Payroll>();

    public virtual ICollection<SalesOrder> SalesOrders { get; set; } = new List<SalesOrder>();

    public virtual ICollection<StockTransaction> StockTransactions { get; set; } = new List<StockTransaction>();

    public virtual ICollection<Stocktake> Stocktakes { get; set; } = new List<Stocktake>();

    public virtual ICollection<UserBranch> UserBranches { get; set; } = new List<UserBranch>();
}
