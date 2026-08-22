using System;
using System.Collections.Generic;

namespace TuanKietBranchFlow.Infrastructure.Models;

public partial class Payroll
{
    public int Id { get; set; }

    public int EmployeeProfileId { get; set; }

    public int BranchId { get; set; }

    public short Year { get; set; }

    public byte Month { get; set; }

    public decimal BaseSalarySnapshot { get; set; }

    public decimal WorkDays { get; set; }

    public decimal OvertimeHours { get; set; }

    public decimal LeaveDays { get; set; }

    public decimal TotalSalary { get; set; }

    public string Status { get; set; } = null!;

    public DateTime? PaidAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public byte[] RowVersion { get; set; } = null!;

    public virtual Branch Branch { get; set; } = null!;

    public virtual EmployeeProfile EmployeeProfile { get; set; } = null!;
}
