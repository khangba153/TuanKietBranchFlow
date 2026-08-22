using System;
using System.Collections.Generic;

namespace TuanKietBranchFlow.Infrastructure.Models;

public partial class OrderItemNote
{
    public int Id { get; set; }

    public int OrderItemId { get; set; }

    public int NoteOptionId { get; set; }

    public string NoteNameSnapshot { get; set; } = null!;

    public virtual NoteOption NoteOption { get; set; } = null!;

    public virtual OrderItem OrderItem { get; set; } = null!;
}
