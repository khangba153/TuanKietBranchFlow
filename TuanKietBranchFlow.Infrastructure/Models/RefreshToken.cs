using System;
using System.Collections.Generic;

namespace TuanKietBranchFlow.Infrastructure.Models;

public partial class RefreshToken
{
    public Guid Id { get; set; }

    public Guid SessionId { get; set; }

    public string TokenHash { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime ExpiresAt { get; set; }

    public DateTime? UsedAt { get; set; }

    public DateTime? RevokedAt { get; set; }

    public byte[] RowVersion { get; set; } = null!;

    public virtual AuthSession Session { get; set; } = null!;
}
