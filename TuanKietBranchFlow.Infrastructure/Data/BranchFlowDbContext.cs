using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using TuanKietBranchFlow.Infrastructure.Models;

namespace TuanKietBranchFlow.Infrastructure.Data;

public partial class BranchFlowDbContext : DbContext
{
    public BranchFlowDbContext(DbContextOptions<BranchFlowDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AppUser> AppUsers { get; set; }

    public virtual DbSet<AuditLog> AuditLogs { get; set; }

    public virtual DbSet<Branch> Branches { get; set; }

    public virtual DbSet<BranchIngredient> BranchIngredients { get; set; }

    public virtual DbSet<BranchProduct> BranchProducts { get; set; }

    public virtual DbSet<BranchTopping> BranchToppings { get; set; }

    public virtual DbSet<Category> Categories { get; set; }

    public virtual DbSet<EmployeeProfile> EmployeeProfiles { get; set; }

    public virtual DbSet<Ingredient> Ingredients { get; set; }

    public virtual DbSet<IngredientUnit> IngredientUnits { get; set; }

    public virtual DbSet<NoteGroup> NoteGroups { get; set; }

    public virtual DbSet<NoteOption> NoteOptions { get; set; }

    public virtual DbSet<OrderAdjustment> OrderAdjustments { get; set; }

    public virtual DbSet<OrderDailyCounter> OrderDailyCounters { get; set; }

    public virtual DbSet<OrderItem> OrderItems { get; set; }

    public virtual DbSet<OrderItemNote> OrderItemNotes { get; set; }

    public virtual DbSet<OrderItemTopping> OrderItemToppings { get; set; }

    public virtual DbSet<Payroll> Payrolls { get; set; }

    public virtual DbSet<Product> Products { get; set; }

    public virtual DbSet<ProductSize> ProductSizes { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<SalesOrder> SalesOrders { get; set; }

    public virtual DbSet<Size> Sizes { get; set; }

    public virtual DbSet<StockTransaction> StockTransactions { get; set; }

    public virtual DbSet<StockTransactionDetail> StockTransactionDetails { get; set; }

    public virtual DbSet<Stocktake> Stocktakes { get; set; }

    public virtual DbSet<StocktakeItem> StocktakeItems { get; set; }

    public virtual DbSet<Topping> Toppings { get; set; }

    public virtual DbSet<ToppingGroup> ToppingGroups { get; set; }

    public virtual DbSet<UserBranch> UserBranches { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.UseCollation("Vietnamese_100_CI_AI");

        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.ToTable("AppUser");

            entity.HasIndex(e => e.RoleId, "IX_AppUser_RoleId");

            entity.HasIndex(e => e.Email, "UX_AppUser_Email")
                .IsUnique()
                .HasFilter("([Email] IS NOT NULL AND [Deleted]=(0))");

            entity.HasIndex(e => e.Username, "UX_AppUser_Username")
                .IsUnique()
                .HasFilter("([Deleted]=(0))");

            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_AppUser_CreatedAt");
            entity.Property(e => e.Email).HasMaxLength(254);
            entity.Property(e => e.FullName).HasMaxLength(150);
            entity.Property(e => e.IsActive).HasDefaultValue(true, "DF_AppUser_IsActive");
            entity.Property(e => e.PasswordHash).HasMaxLength(500);
            entity.Property(e => e.Phone).HasMaxLength(30);
            entity.Property(e => e.UpdatedAt).HasPrecision(0);
            entity.Property(e => e.Username).HasMaxLength(100);

            entity.HasOne(d => d.Role).WithMany(p => p.AppUsers)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AppUser_Role");
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.ToTable("AuditLog");

            entity.HasIndex(e => new { e.BranchId, e.CreatedAt }, "IX_AuditLog_Branch_Date").IsDescending(false, true);

            entity.HasIndex(e => new { e.EntityName, e.EntityId, e.CreatedAt }, "IX_AuditLog_Entity_Date").IsDescending(false, false, true);

            entity.Property(e => e.Action).HasMaxLength(20);
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_AuditLog_CreatedAt");
            entity.Property(e => e.EntityName).HasMaxLength(100);

            entity.HasOne(d => d.Branch).WithMany(p => p.AuditLogs)
                .HasForeignKey(d => d.BranchId)
                .HasConstraintName("FK_AuditLog_Branch");

            entity.HasOne(d => d.PerformedByUser).WithMany(p => p.AuditLogs)
                .HasForeignKey(d => d.PerformedByUserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AuditLog_PerformedByUser");
        });

        modelBuilder.Entity<Branch>(entity =>
        {
            entity.ToTable("Branch");

            entity.HasIndex(e => e.Code, "UX_Branch_Code")
                .IsUnique()
                .HasFilter("([Deleted]=(0))");

            entity.Property(e => e.Address).HasMaxLength(500);
            entity.Property(e => e.Code).HasMaxLength(30);
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_Branch_CreatedAt");
            entity.Property(e => e.IsActive).HasDefaultValue(true, "DF_Branch_IsActive");
            entity.Property(e => e.Name).HasMaxLength(150);
            entity.Property(e => e.UpdatedAt).HasPrecision(0);
        });

        modelBuilder.Entity<BranchIngredient>(entity =>
        {
            entity.ToTable("BranchIngredient");

            entity.HasIndex(e => new { e.BranchId, e.IngredientId }, "UQ_BranchIngredient_Branch_Ingredient").IsUnique();

            entity.Property(e => e.Quantity).HasColumnType("decimal(18, 3)");
            entity.Property(e => e.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken();
            entity.Property(e => e.UpdatedAt).HasPrecision(0);
            entity.Property(e => e.WarningThreshold).HasColumnType("decimal(18, 3)");

            entity.HasOne(d => d.Branch).WithMany(p => p.BranchIngredients)
                .HasForeignKey(d => d.BranchId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BranchIngredient_Branch");

            entity.HasOne(d => d.Ingredient).WithMany(p => p.BranchIngredients)
                .HasForeignKey(d => d.IngredientId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BranchIngredient_Ingredient");
        });

        modelBuilder.Entity<BranchProduct>(entity =>
        {
            entity.ToTable("BranchProduct");

            entity.HasIndex(e => new { e.BranchId, e.ProductId }, "UQ_BranchProduct_Branch_Product").IsUnique();

            entity.Property(e => e.IsAvailable).HasDefaultValue(true, "DF_BranchProduct_IsAvailable");
            entity.Property(e => e.UpdatedAt).HasPrecision(0);

            entity.HasOne(d => d.Branch).WithMany(p => p.BranchProducts)
                .HasForeignKey(d => d.BranchId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BranchProduct_Branch");

            entity.HasOne(d => d.Product).WithMany(p => p.BranchProducts)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BranchProduct_Product");
        });

        modelBuilder.Entity<BranchTopping>(entity =>
        {
            entity.ToTable("BranchTopping");

            entity.HasIndex(e => new { e.BranchId, e.ToppingId }, "UQ_BranchTopping_Branch_Topping").IsUnique();

            entity.Property(e => e.IsAvailable).HasDefaultValue(true, "DF_BranchTopping_IsAvailable");
            entity.Property(e => e.UpdatedAt).HasPrecision(0);

            entity.HasOne(d => d.Branch).WithMany(p => p.BranchToppings)
                .HasForeignKey(d => d.BranchId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BranchTopping_Branch");

            entity.HasOne(d => d.Topping).WithMany(p => p.BranchToppings)
                .HasForeignKey(d => d.ToppingId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BranchTopping_Topping");
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.ToTable("Category");

            entity.HasIndex(e => e.Name, "UX_Category_Name")
                .IsUnique()
                .HasFilter("([Deleted]=(0))");

            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_Category_CreatedAt");
            entity.Property(e => e.IsActive).HasDefaultValue(true, "DF_Category_IsActive");
            entity.Property(e => e.Name).HasMaxLength(150);
            entity.Property(e => e.UpdatedAt).HasPrecision(0);
        });

        modelBuilder.Entity<EmployeeProfile>(entity =>
        {
            entity.ToTable("EmployeeProfile");

            entity.HasIndex(e => e.EmployeeCode, "UX_EmployeeProfile_EmployeeCode")
                .IsUnique()
                .HasFilter("([Deleted]=(0))");

            entity.HasIndex(e => e.UserId, "UX_EmployeeProfile_User")
                .IsUnique()
                .HasFilter("([Deleted]=(0))");

            entity.Property(e => e.Address).HasMaxLength(500);
            entity.Property(e => e.AvatarUrl).HasMaxLength(1000);
            entity.Property(e => e.BaseSalary).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_EmployeeProfile_CreatedAt");
            entity.Property(e => e.EmployeeCode).HasMaxLength(30);
            entity.Property(e => e.Position).HasMaxLength(100);
            entity.Property(e => e.UpdatedAt).HasPrecision(0);

            entity.HasOne(d => d.User).WithOne(p => p.EmployeeProfile)
                .HasForeignKey<EmployeeProfile>(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_EmployeeProfile_User");
        });

        modelBuilder.Entity<Ingredient>(entity =>
        {
            entity.ToTable("Ingredient");

            entity.HasIndex(e => e.Name, "UX_Ingredient_Name")
                .IsUnique()
                .HasFilter("([Deleted]=(0))");

            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_Ingredient_CreatedAt");
            entity.Property(e => e.IsActive).HasDefaultValue(true, "DF_Ingredient_IsActive");
            entity.Property(e => e.Name).HasMaxLength(150);
            entity.Property(e => e.UpdatedAt).HasPrecision(0);

            entity.HasOne(d => d.Unit).WithMany(p => p.Ingredients)
                .HasForeignKey(d => d.UnitId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Ingredient_Unit");
        });

        modelBuilder.Entity<IngredientUnit>(entity =>
        {
            entity.ToTable("IngredientUnit");

            entity.HasIndex(e => e.Name, "UX_IngredientUnit_Name")
                .IsUnique()
                .HasFilter("([Deleted]=(0))");

            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_IngredientUnit_CreatedAt");
            entity.Property(e => e.IsActive).HasDefaultValue(true, "DF_IngredientUnit_IsActive");
            entity.Property(e => e.Name).HasMaxLength(50);
            entity.Property(e => e.UpdatedAt).HasPrecision(0);
        });

        modelBuilder.Entity<NoteGroup>(entity =>
        {
            entity.ToTable("NoteGroup");

            entity.HasIndex(e => e.Name, "UX_NoteGroup_Name")
                .IsUnique()
                .HasFilter("([Deleted]=(0))");

            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_NoteGroup_CreatedAt");
            entity.Property(e => e.IsActive).HasDefaultValue(true, "DF_NoteGroup_IsActive");
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.UpdatedAt).HasPrecision(0);
        });

        modelBuilder.Entity<NoteOption>(entity =>
        {
            entity.ToTable("NoteOption");

            entity.HasIndex(e => new { e.NoteGroupId, e.Name }, "UX_NoteOption_Group_Name")
                .IsUnique()
                .HasFilter("([Deleted]=(0))");

            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_NoteOption_CreatedAt");
            entity.Property(e => e.IsActive).HasDefaultValue(true, "DF_NoteOption_IsActive");
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.UpdatedAt).HasPrecision(0);

            entity.HasOne(d => d.NoteGroup).WithMany(p => p.NoteOptions)
                .HasForeignKey(d => d.NoteGroupId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_NoteOption_NoteGroup");
        });

        modelBuilder.Entity<OrderAdjustment>(entity =>
        {
            entity.ToTable("OrderAdjustment");

            entity.HasIndex(e => new { e.SalesOrderId, e.CreatedAt }, "IX_OrderAdjustment_SalesOrder_Date").IsDescending(false, true);

            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_OrderAdjustment_CreatedAt");
            entity.Property(e => e.Reason).HasMaxLength(500);

            entity.HasOne(d => d.AdjustedByUser).WithMany(p => p.OrderAdjustments)
                .HasForeignKey(d => d.AdjustedByUserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrderAdjustment_AdjustedByUser");

            entity.HasOne(d => d.SalesOrder).WithMany(p => p.OrderAdjustments)
                .HasForeignKey(d => d.SalesOrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrderAdjustment_SalesOrder");
        });

        modelBuilder.Entity<OrderDailyCounter>(entity =>
        {
            entity.HasKey(e => new { e.BranchId, e.BusinessDate });

            entity.ToTable("OrderDailyCounter");

            entity.Property(e => e.UpdatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_OrderDailyCounter_UpdatedAt");

            entity.HasOne(d => d.Branch).WithMany(p => p.OrderDailyCounters)
                .HasForeignKey(d => d.BranchId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrderDailyCounter_Branch");
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.ToTable("OrderItem");

            entity.HasIndex(e => e.SalesOrderId, "IX_OrderItem_SalesOrderId");

            entity.Property(e => e.ProductNameSnapshot).HasMaxLength(150);
            entity.Property(e => e.SizeNameSnapshot).HasMaxLength(50);
            entity.Property(e => e.SubtotalAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.UnitPriceSnapshot).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.ProductSize).WithMany(p => p.OrderItems)
                .HasForeignKey(d => d.ProductSizeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrderItem_ProductSize");

            entity.HasOne(d => d.SalesOrder).WithMany(p => p.OrderItems)
                .HasForeignKey(d => d.SalesOrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrderItem_SalesOrder");
        });

        modelBuilder.Entity<OrderItemNote>(entity =>
        {
            entity.ToTable("OrderItemNote");

            entity.HasIndex(e => new { e.OrderItemId, e.NoteOptionId }, "UQ_OrderItemNote_Item_Note").IsUnique();

            entity.Property(e => e.NoteNameSnapshot).HasMaxLength(100);

            entity.HasOne(d => d.NoteOption).WithMany(p => p.OrderItemNotes)
                .HasForeignKey(d => d.NoteOptionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrderItemNote_NoteOption");

            entity.HasOne(d => d.OrderItem).WithMany(p => p.OrderItemNotes)
                .HasForeignKey(d => d.OrderItemId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrderItemNote_OrderItem");
        });

        modelBuilder.Entity<OrderItemTopping>(entity =>
        {
            entity.ToTable("OrderItemTopping");

            entity.HasIndex(e => new { e.OrderItemId, e.ToppingId }, "UQ_OrderItemTopping_Item_Topping").IsUnique();

            entity.Property(e => e.Quantity).HasDefaultValue(1, "DF_OrderItemTopping_Quantity");
            entity.Property(e => e.ToppingNameSnapshot).HasMaxLength(150);
            entity.Property(e => e.UnitPriceSnapshot).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.OrderItem).WithMany(p => p.OrderItemToppings)
                .HasForeignKey(d => d.OrderItemId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrderItemTopping_OrderItem");

            entity.HasOne(d => d.Topping).WithMany(p => p.OrderItemToppings)
                .HasForeignKey(d => d.ToppingId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrderItemTopping_Topping");
        });

        modelBuilder.Entity<Payroll>(entity =>
        {
            entity.ToTable("Payroll");

            entity.HasIndex(e => new { e.EmployeeProfileId, e.Year, e.Month }, "UQ_Payroll_Employee_Year_Month").IsUnique();

            entity.Property(e => e.BaseSalarySnapshot).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_Payroll_CreatedAt");
            entity.Property(e => e.LeaveDays).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.OvertimeHours).HasColumnType("decimal(6, 2)");
            entity.Property(e => e.PaidAt).HasPrecision(0);
            entity.Property(e => e.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken();
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("DRAFT", "DF_Payroll_Status");
            entity.Property(e => e.TotalSalary).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.UpdatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_Payroll_UpdatedAt");
            entity.Property(e => e.WorkDays).HasColumnType("decimal(5, 2)");

            entity.HasOne(d => d.Branch).WithMany(p => p.Payrolls)
                .HasForeignKey(d => d.BranchId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Payroll_Branch");

            entity.HasOne(d => d.EmployeeProfile).WithMany(p => p.Payrolls)
                .HasForeignKey(d => d.EmployeeProfileId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Payroll_EmployeeProfile");
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable("Product");

            entity.HasIndex(e => new { e.CategoryId, e.IsActive }, "IX_Product_Category");

            entity.HasIndex(e => e.Name, "UX_Product_Name")
                .IsUnique()
                .HasFilter("([Deleted]=(0))");

            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_Product_CreatedAt");
            entity.Property(e => e.ImageUrl).HasMaxLength(1000);
            entity.Property(e => e.IsActive).HasDefaultValue(true, "DF_Product_IsActive");
            entity.Property(e => e.Name).HasMaxLength(150);
            entity.Property(e => e.UpdatedAt).HasPrecision(0);

            entity.HasOne(d => d.Category).WithMany(p => p.Products)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Product_Category");
        });

        modelBuilder.Entity<ProductSize>(entity =>
        {
            entity.ToTable("ProductSize");

            entity.HasIndex(e => new { e.ProductId, e.IsActive }, "IX_ProductSize_Product");

            entity.HasIndex(e => new { e.ProductId, e.SizeId }, "UX_ProductSize_Product_Size")
                .IsUnique()
                .HasFilter("([Deleted]=(0))");

            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_ProductSize_CreatedAt");
            entity.Property(e => e.IsActive).HasDefaultValue(true, "DF_ProductSize_IsActive");
            entity.Property(e => e.Price).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.UpdatedAt).HasPrecision(0);

            entity.HasOne(d => d.Product).WithMany(p => p.ProductSizes)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ProductSize_Product");

            entity.HasOne(d => d.Size).WithMany(p => p.ProductSizes)
                .HasForeignKey(d => d.SizeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ProductSize_Size");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("Role");

            entity.HasIndex(e => e.Code, "UX_Role_Code")
                .IsUnique()
                .HasFilter("([Deleted]=(0))");

            entity.Property(e => e.Code).HasMaxLength(30);
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_Role_CreatedAt");
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.UpdatedAt).HasPrecision(0);
        });

        modelBuilder.Entity<SalesOrder>(entity =>
        {
            entity.ToTable("SalesOrder");

            entity.HasIndex(e => new { e.BranchId, e.BusinessDate, e.Status }, "IX_SalesOrder_Branch_Date_Status");

            entity.HasIndex(e => new { e.CreatedByUserId, e.CreatedAt }, "IX_SalesOrder_CreatedByUser_Date").IsDescending(false, true);

            entity.HasIndex(e => new { e.BranchId, e.BusinessDate, e.DailySequence }, "UQ_SalesOrder_Branch_Date_Sequence").IsUnique();

            entity.HasIndex(e => e.Code, "UQ_SalesOrder_Code").IsUnique();

            entity.Property(e => e.Code).HasMaxLength(50);
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_SalesOrder_CreatedAt");
            entity.Property(e => e.ReportReason).HasMaxLength(500);
            entity.Property(e => e.ReportedAt).HasPrecision(0);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("COMPLETED", "DF_SalesOrder_Status");
            entity.Property(e => e.TotalAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.UpdatedAt).HasPrecision(0);

            entity.HasOne(d => d.Branch).WithMany(p => p.SalesOrders)
                .HasForeignKey(d => d.BranchId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SalesOrder_Branch");

            entity.HasOne(d => d.CreatedByUser).WithMany(p => p.SalesOrderCreatedByUsers)
                .HasForeignKey(d => d.CreatedByUserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SalesOrder_CreatedByUser");

            entity.HasOne(d => d.ReportedByUser).WithMany(p => p.SalesOrderReportedByUsers)
                .HasForeignKey(d => d.ReportedByUserId)
                .HasConstraintName("FK_SalesOrder_ReportedByUser");
        });

        modelBuilder.Entity<Size>(entity =>
        {
            entity.ToTable("Size");

            entity.HasIndex(e => e.Name, "UX_Size_Name")
                .IsUnique()
                .HasFilter("([Deleted]=(0))");

            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_Size_CreatedAt");
            entity.Property(e => e.IsActive).HasDefaultValue(true, "DF_Size_IsActive");
            entity.Property(e => e.Name).HasMaxLength(50);
            entity.Property(e => e.UpdatedAt).HasPrecision(0);
        });

        modelBuilder.Entity<StockTransaction>(entity =>
        {
            entity.ToTable("StockTransaction");

            entity.HasIndex(e => new { e.BranchId, e.CreatedAt }, "IX_StockTransaction_Branch_Date").IsDescending(false, true);

            entity.HasIndex(e => e.OriginalTransactionId, "UX_StockTransaction_OriginalTransaction")
                .IsUnique()
                .HasFilter("([OriginalTransactionId] IS NOT NULL)");

            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_StockTransaction_CreatedAt");
            entity.Property(e => e.Type).HasMaxLength(30);

            entity.HasOne(d => d.Branch).WithMany(p => p.StockTransactions)
                .HasForeignKey(d => d.BranchId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_StockTransaction_Branch");

            entity.HasOne(d => d.OriginalTransaction).WithOne(p => p.InverseOriginalTransaction)
                .HasForeignKey<StockTransaction>(d => d.OriginalTransactionId)
                .HasConstraintName("FK_StockTransaction_OriginalTransaction");

            entity.HasOne(d => d.PerformedByUser).WithMany(p => p.StockTransactions)
                .HasForeignKey(d => d.PerformedByUserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_StockTransaction_PerformedByUser");
        });

        modelBuilder.Entity<StockTransactionDetail>(entity =>
        {
            entity.ToTable("StockTransactionDetail");

            entity.HasIndex(e => new { e.StockTransactionId, e.IngredientId }, "UQ_StockTransactionDetail_Transaction_Ingredient").IsUnique();

            entity.Property(e => e.QuantityAfter).HasColumnType("decimal(18, 3)");
            entity.Property(e => e.QuantityBefore).HasColumnType("decimal(18, 3)");
            entity.Property(e => e.QuantityChange).HasColumnType("decimal(18, 3)");

            entity.HasOne(d => d.Ingredient).WithMany(p => p.StockTransactionDetails)
                .HasForeignKey(d => d.IngredientId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_StockTransactionDetail_Ingredient");

            entity.HasOne(d => d.StockTransaction).WithMany(p => p.StockTransactionDetails)
                .HasForeignKey(d => d.StockTransactionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_StockTransactionDetail_StockTransaction");
        });

        modelBuilder.Entity<Stocktake>(entity =>
        {
            entity.ToTable("Stocktake");

            entity.HasIndex(e => new { e.BranchId, e.CompletedAt }, "IX_Stocktake_Branch_Date").IsDescending(false, true);

            entity.HasIndex(e => e.Code, "UQ_Stocktake_Code").IsUnique();

            entity.HasIndex(e => e.AdjustmentTransactionId, "UX_Stocktake_AdjustmentTransaction")
                .IsUnique()
                .HasFilter("([AdjustmentTransactionId] IS NOT NULL)");

            entity.Property(e => e.Code).HasMaxLength(50);
            entity.Property(e => e.CompletedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_Stocktake_CompletedAt");

            entity.HasOne(d => d.AdjustmentTransaction).WithOne(p => p.Stocktake)
                .HasForeignKey<Stocktake>(d => d.AdjustmentTransactionId)
                .HasConstraintName("FK_Stocktake_AdjustmentTransaction");

            entity.HasOne(d => d.Branch).WithMany(p => p.Stocktakes)
                .HasForeignKey(d => d.BranchId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Stocktake_Branch");

            entity.HasOne(d => d.CheckedByUser).WithMany(p => p.Stocktakes)
                .HasForeignKey(d => d.CheckedByUserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Stocktake_CheckedByUser");
        });

        modelBuilder.Entity<StocktakeItem>(entity =>
        {
            entity.ToTable("StocktakeItem");

            entity.HasIndex(e => new { e.StocktakeId, e.IngredientId }, "UQ_StocktakeItem_Stocktake_Ingredient").IsUnique();

            entity.Property(e => e.ActualQuantity).HasColumnType("decimal(18, 3)");
            entity.Property(e => e.Difference)
                .HasComputedColumnSql("([ActualQuantity]-[SystemQuantity])", true)
                .HasColumnType("decimal(19, 3)");
            entity.Property(e => e.SystemQuantity).HasColumnType("decimal(18, 3)");

            entity.HasOne(d => d.Ingredient).WithMany(p => p.StocktakeItems)
                .HasForeignKey(d => d.IngredientId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_StocktakeItem_Ingredient");

            entity.HasOne(d => d.Stocktake).WithMany(p => p.StocktakeItems)
                .HasForeignKey(d => d.StocktakeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_StocktakeItem_Stocktake");
        });

        modelBuilder.Entity<Topping>(entity =>
        {
            entity.ToTable("Topping");

            entity.HasIndex(e => e.Name, "UX_Topping_Name")
                .IsUnique()
                .HasFilter("([Deleted]=(0))");

            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_Topping_CreatedAt");
            entity.Property(e => e.IsActive).HasDefaultValue(true, "DF_Topping_IsActive");
            entity.Property(e => e.Name).HasMaxLength(150);
            entity.Property(e => e.Price).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.UpdatedAt).HasPrecision(0);

            entity.HasOne(d => d.ToppingGroup).WithMany(p => p.Toppings)
                .HasForeignKey(d => d.ToppingGroupId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Topping_ToppingGroup");
        });

        modelBuilder.Entity<ToppingGroup>(entity =>
        {
            entity.ToTable("ToppingGroup");

            entity.HasIndex(e => e.Name, "UX_ToppingGroup_Name")
                .IsUnique()
                .HasFilter("([Deleted]=(0))");

            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_ToppingGroup_CreatedAt");
            entity.Property(e => e.IsActive).HasDefaultValue(true, "DF_ToppingGroup_IsActive");
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.UpdatedAt).HasPrecision(0);
        });

        modelBuilder.Entity<UserBranch>(entity =>
        {
            entity.ToTable("UserBranch");

            entity.HasIndex(e => new { e.BranchId, e.ActiveTo }, "IX_UserBranch_Branch_Active");

            entity.HasIndex(e => new { e.UserId, e.BranchId }, "UX_UserBranch_Current")
                .IsUnique()
                .HasFilter("([ActiveTo] IS NULL)");

            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_UserBranch_CreatedAt");

            entity.HasOne(d => d.Branch).WithMany(p => p.UserBranches)
                .HasForeignKey(d => d.BranchId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UserBranch_Branch");

            entity.HasOne(d => d.User).WithMany(p => p.UserBranches)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UserBranch_User");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
