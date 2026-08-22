using System;
using System.Collections.Generic;

namespace TuanKietBranchFlow.Infrastructure.Models;

public partial class AuditLog
{
    public int Id { get; set; }

    public int? BranchId { get; set; }

    public string EntityName { get; set; } = null!;

    public int EntityId { get; set; }

    public string Action { get; set; } = null!;

    public string? BeforeData { get; set; }

    public string? AfterData { get; set; }

    public int PerformedByUserId { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Branch? Branch { get; set; }

    public virtual AppUser PerformedByUser { get; set; } = null!;
}
