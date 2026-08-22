using System;
using System.Collections.Generic;

namespace TuanKietBranchFlow.Infrastructure.Models;

public partial class NoteOption
{
    public int Id { get; set; }

    public int NoteGroupId { get; set; }

    public string Name { get; set; } = null!;

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public bool Deleted { get; set; }

    public virtual NoteGroup NoteGroup { get; set; } = null!;

    public virtual ICollection<OrderItemNote> OrderItemNotes { get; set; } = new List<OrderItemNote>();
}
