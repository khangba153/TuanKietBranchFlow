using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore;
using TuanKietBranchFlow.Infrastructure.Data;
using TuanKietBranchFlow.Infrastructure.Models;

namespace TuanKietBranchFlow.Infrastructure.Repositories;

public class OrderRepository : RepositoryBase<SalesOrder>, IOrderRepository
{
    public OrderRepository(BranchFlowDbContext context) : base(context)
    {
    }

    // Lấy danh sách các món-size hợp lệ và đang được bán tại chi nhánh
    public async Task<List<ProductSize>> GetAvailableProductSizesAsync(
        int branchId, List<int> productSizeIds)
    {
        return await Context.ProductSizes
            .AsNoTracking()
            .Where(productSize =>
                productSizeIds.Contains(productSize.Id)
                && productSize.IsActive
                && !productSize.Deleted
                && productSize.Product.IsActive
                && !productSize.Product.Deleted
                && productSize.Size.IsActive
                && !productSize.Size.Deleted
                && productSize.Product.BranchProducts.Any(branchProduct =>
                    branchProduct.BranchId == branchId
                    && branchProduct.IsAvailable))
            .Include(productSize => productSize.Product)
            .Include(productSize => productSize.Size)
            .ToListAsync();
    }

    // Lấy danh sách các topping hợp lệ và đang được bán tại chi nhánh
    public async Task<List<Topping>> GetAvailableToppingsAsync(
        int branchId, List<int> toppingIds)
    {
        return await Context.Toppings
            .AsNoTracking()
            .Where(topping =>
                toppingIds.Contains(topping.Id)
                && topping.IsActive
                && !topping.Deleted
                && topping.ToppingGroup.IsActive
                && !topping.ToppingGroup.Deleted
                && topping.BranchToppings.Any(branchTopping =>
                    branchTopping.BranchId == branchId
                    && branchTopping.IsAvailable))
            .Include(topping => topping.ToppingGroup)
            .ToListAsync();
    }

    // Lấy danh sách các lựa chọn ghi chú đang hoạt động
    public async Task<List<NoteOption>> GetActiveNoteOptionsAsync(
        List<int> noteOptionIds)
    {
        return await Context.NoteOptions
            .AsNoTracking()
            .Where(noteOption =>
                noteOptionIds.Contains(noteOption.Id)
                && noteOption.IsActive
                && !noteOption.Deleted
                && noteOption.NoteGroup.IsActive
                && !noteOption.NoteGroup.Deleted)
            .Include(noteOption => noteOption.NoteGroup)
            .ToListAsync();
    }

    // Gọi stored procedure để lấy mã đơn và số thứ tự không bị trùng
    public async Task<OrderCodeResult> GetNextOrderCodeAsync(
        int branchId, DateOnly businessDate)
    {
        DbConnection connection = Context.Database.GetDbConnection();

        // Chỉ đóng connection nếu chính method này đã mở nó
        bool shouldCloseConnection = connection.State != ConnectionState.Open;

        if (shouldCloseConnection)
        {
            await connection.OpenAsync();
        }

        try
        {
            await using DbCommand command = connection.CreateCommand();

            command.CommandText = "dbo.usp_GetNextOrderCode";
            command.CommandType = CommandType.StoredProcedure;

            // Nếu Service đã mở trong transaction thì procedure phải tham gia transaction đó
            command.Transaction =
                Context.Database.CurrentTransaction?.GetDbTransaction();

            DbParameter branchIdParameter = command.CreateParameter();
            branchIdParameter.ParameterName = "@BranchId";
            branchIdParameter.DbType = DbType.Int32;
            branchIdParameter.Value = branchId;
            command.Parameters.Add(branchIdParameter);

            DbParameter businessDateParameter = command.CreateParameter();
            businessDateParameter.ParameterName = "@BusinessDate";
            businessDateParameter.DbType = DbType.Date;
            businessDateParameter.Value = businessDate.ToDateTime(TimeOnly.MinValue);
            command.Parameters.Add(businessDateParameter);

            DbParameter orderCodeParameter = command.CreateParameter();
            orderCodeParameter.ParameterName = "@OrderCode";
            orderCodeParameter.DbType = DbType.String;
            orderCodeParameter.Size = 50;
            orderCodeParameter.Direction = ParameterDirection.Output;
            command.Parameters.Add(orderCodeParameter);

            DbParameter sequenceNumberParameter = command.CreateParameter();
            sequenceNumberParameter.ParameterName = "@SequenceNumber";
            sequenceNumberParameter.DbType = DbType.Int32;
            sequenceNumberParameter.Direction = ParameterDirection.Output;
            command.Parameters.Add(sequenceNumberParameter);

            await command.ExecuteNonQueryAsync();

            return new OrderCodeResult
            {
                Code = Convert.ToString(orderCodeParameter.Value) ?? string.Empty,
                SequenceNumber = Convert.ToInt32(sequenceNumberParameter.Value)
            };
        }
        finally
        {
            if (shouldCloseConnection)
            {
                await connection.CloseAsync();
            }
        }
    }

    // Lấy lịch sử đơn do chính employee hiện tại tạo
    public async Task<List<SalesOrder>> GetOrdersCreatedByUserAsync(
        int currentUserId,
        string keyword,
        DateOnly? businessDate)
    {
        IQueryable<SalesOrder> query = Context.SalesOrders
            .AsNoTracking()
            .Where(salesOrder =>
                salesOrder.CreatedByUserId == currentUserId);

        // Chỉ lọc mã đơn khi người dùng nhập từ khóa
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query = query.Where(salesOrder =>
                salesOrder.Code.Contains(keyword));
        }

        // Web truyền ngày hiện tại khi người dùng chọn lọc bộ lọc "Hôm nay"
        if (businessDate.HasValue)
        {
            query = query.Where(salesOrder =>
                salesOrder.BusinessDate == businessDate.Value);
        }

        return await query
            .Include(salesOrder => salesOrder.OrderItems)
            .OrderByDescending(salesOrder => salesOrder.CreatedAt)
            .ToListAsync();
    }

    // Lấy chi tiết 1 đơn do chính employee hiện tại tạo
    public async Task<SalesOrder?> GetOrderDetailCreatedByUserAsync(
        int orderId,
        int currentUserId)
    {
        return await Context.SalesOrders
            .AsNoTracking()
            .Where(salesOrder =>
                salesOrder.Id == orderId
                && salesOrder.CreatedByUserId == currentUserId)
            .Include(salesOrder => salesOrder.Branch)
            .Include(salesOrder => salesOrder.OrderItems)
                .ThenInclude(orderItem => orderItem.OrderItemToppings)
            .Include(salesOrder => salesOrder.OrderItems)
                .ThenInclude(orderItem => orderItem.OrderItemNotes)
            .FirstOrDefaultAsync();
    }

    // Lấy entity có tracking để Service cập nhật thông tin báo sai
    public async Task<SalesOrder?> GetOrderForReportAsync(
        int orderId,
        int currentUserId)
    {
        return await Context.SalesOrders
            .Where(salesOrder =>
                salesOrder.Id == orderId
                && salesOrder.CreatedByUserId == currentUserId)
            .FirstOrDefaultAsync();
    }
}