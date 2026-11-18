using Microsoft.EntityFrameworkCore;
using TuanKietBranchFlow.Infrastructure.Models;

namespace TuanKietBranchFlow.Infrastructure.Data;

public partial class BranchFlowDbContext
{
    public virtual DbSet<EmployeeTransferRequest> EmployeeTransferRequests { get; set; }

    // Mapping từ scaffold bảng mới; không ghi đè các model Database First cũ.
    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EmployeeTransferRequest>(entity =>
        {
            entity.ToTable("EmployeeTransferRequest");
            entity.HasIndex(e => new { e.SourceBranchId, e.RequestedAt }, "IX_EmployeeTransferRequest_Source").IsDescending(false, true);
            entity.HasIndex(e => new { e.TargetBranchId, e.RequestedAt }, "IX_EmployeeTransferRequest_Target").IsDescending(false, true);
            entity.HasIndex(e => e.EmployeeId, "UX_EmployeeTransferRequest_PendingEmployee").IsUnique().HasFilter("([Status]='PENDING')");
            entity.Property(e => e.EmployeeCode).HasMaxLength(50);
            entity.Property(e => e.EmployeeName).HasMaxLength(200);
            entity.Property(e => e.Note).HasMaxLength(500);
            entity.Property(e => e.ProcessedByName).HasMaxLength(200);
            entity.Property(e => e.RequestedByName).HasMaxLength(200);
            entity.Property(e => e.ResolutionReason).HasMaxLength(500);
            entity.Property(e => e.SourceBranchName).HasMaxLength(200);
            entity.Property(e => e.TargetBranchName).HasMaxLength(200);
            entity.Property(e => e.Status).HasMaxLength(20).IsUnicode(false);
            entity.Property(e => e.RequestedAt).HasDefaultValueSql("(sysutcdatetime())", "DF_EmployeeTransferRequest_RequestedAt");
            entity.Property(e => e.RowVersion).IsRowVersion().IsConcurrencyToken();
        });
    }
}
