using System;
using System.Collections.Generic;

namespace TuanKietBranchFlow.Infrastructure.Models;

public partial class EmployeeProfile
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string EmployeeCode { get; set; } = null!;

    public DateOnly? DateOfBirth { get; set; }

    public DateOnly HireDate { get; set; }

    public DateOnly? LeaveDate { get; set; }

    public string? Position { get; set; }

    public string? Address { get; set; }

    public string? AvatarUrl { get; set; }

    public decimal BaseSalary { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public bool Deleted { get; set; }

    public virtual ICollection<Payroll> Payrolls { get; set; } = new List<Payroll>();

    public virtual AppUser User { get; set; } = null!;
}
