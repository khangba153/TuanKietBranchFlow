using System;
using System.Collections.Generic;

namespace TuanKietBranchFlow.Infrastructure.Models;

public partial class AppUser
{
    public int Id { get; set; }

    public int RoleId { get; set; }

    public string Username { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string FullName { get; set; } = null!;

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public bool Deleted { get; set; }

    public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();

    public virtual EmployeeProfile? EmployeeProfile { get; set; }

    public virtual ICollection<OrderAdjustment> OrderAdjustments { get; set; } = new List<OrderAdjustment>();

    public virtual Role Role { get; set; } = null!;

    public virtual ICollection<SalesOrder> SalesOrderCreatedByUsers { get; set; } = new List<SalesOrder>();

    public virtual ICollection<SalesOrder> SalesOrderReportedByUsers { get; set; } = new List<SalesOrder>();

    public virtual ICollection<StockTransaction> StockTransactions { get; set; } = new List<StockTransaction>();

    public virtual ICollection<Stocktake> Stocktakes { get; set; } = new List<Stocktake>();

    public virtual ICollection<UserBranch> UserBranches { get; set; } = new List<UserBranch>();
}
