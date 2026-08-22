using System;
using System.Collections.Generic;

namespace TuanKietBranchFlow.Infrastructure.Models;

public partial class UserBranch
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int BranchId { get; set; }

    public DateOnly ActiveFrom { get; set; }

    public DateOnly? ActiveTo { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Branch Branch { get; set; } = null!;

    public virtual AppUser User { get; set; } = null!;
}
