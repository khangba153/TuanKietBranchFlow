using Moq;
using TuanKietBranchFlow.Application.DTOs.Orders;
using TuanKietBranchFlow.Application.Services.Orders;
using TuanKietBranchFlow.Infrastructure.Models;
using TuanKietBranchFlow.Infrastructure.Repositories.Branches;
using TuanKietBranchFlow.Infrastructure.Repositories.Orders;
using TuanKietBranchFlow.Infrastructure.UnitOfWork;

namespace TuanKietBranchFlow.Tests.Application.Orders;

public class EmployeeOrderReadRegressionTests
{
    private readonly Mock<IOrderRepository> _orders = new Mock<IOrderRepository>(MockBehavior.Strict);
    private readonly Mock<IBranchRepository> _branches = new Mock<IBranchRepository>(MockBehavior.Strict);
    private readonly Mock<IUnitOfWork> _work = new Mock<IUnitOfWork>(MockBehavior.Strict);
    private OrderService CreateService() => new(_branches.Object, _orders.Object, _work.Object);
    [Fact]
    public async Task MyOrders_UsesActingEmployeeAndNormalizesKeywordWithoutSaving()
    {
        DateOnly date = new DateOnly(2026, 11, 7);
        _orders.Setup(x => x.GetOrdersCreatedByUserAsync(7, "CN02", date)).ReturnsAsync(new List<SalesOrder>
        {
            MakeOrder()
        });
        List<MyOrderListItemDTO> rows = await CreateService().GetMyOrdersAsync(7, " CN02 ", date);
        MyOrderListItemDTO row = Assert.Single(rows);
        Assert.Equal("TEST-ORDER", row.Code);
        Assert.Equal(3, row.TotalQuantity);
        Assert.Equal(54000m, row.TotalAmount);
        _orders.VerifyAll();
        _branches.VerifyNoOtherCalls();
        _work.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task MyOrders_EmptyScope_ReturnsEmptyWithoutManagementQuery()
    {
        _orders.Setup(x => x.GetOrdersCreatedByUserAsync(7, "", null)).ReturnsAsync(new List<SalesOrder>());
        Assert.Empty(await CreateService().GetMyOrdersAsync(7, "", null));
        _orders.VerifyAll();
        _orders.VerifyNoOtherCalls();
        _work.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task MyDetail_UsesEmployeeScopedQueryAndStoredSnapshots()
    {
        _orders.Setup(x => x.GetOrderDetailCreatedByUserAsync(42, 7)).ReturnsAsync(MakeOrder());
        MyOrderDetailDTO? detail = await CreateService().GetMyOrderDetailAsync(7, 42);
        Assert.NotNull(detail);
        Assert.Equal("CN02", detail.BranchCode);
        MyOrderDetailItemDTO item = Assert.Single(detail.Items);
        Assert.Equal("Tên món lúc bán", item.ProductName);
        Assert.Equal(18000m, item.UnitPrice);
        Assert.Equal(54000m, item.SubtotalAmount);
        _orders.VerifyAll();
        _orders.VerifyNoOtherCalls();
        _work.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task MyDetail_OutsideOwnership_ReturnsNullWithoutSaving()
    {
        _orders.Setup(x => x.GetOrderDetailCreatedByUserAsync(42, 8)).ReturnsAsync((SalesOrder?)null);
        Assert.Null(await CreateService().GetMyOrderDetailAsync(8, 42));
        _orders.VerifyAll();
        _work.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Report_BlankReason_DoesNotQueryOrSave(string reason)
    {
        ReportOrderResult result = await CreateService().ReportOrderAsync(7, 42, new ReportOrderRequestDTO
        {
            Reason = reason
        });
        Assert.False(result.IsReportValid);
        _orders.VerifyNoOtherCalls();
        _work.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Report_AnotherEmployeesOrder_IsNotFoundAndDoesNotSave()
    {
        _orders.Setup(x => x.GetOrderForReportAsync(42, 8)).ReturnsAsync((SalesOrder?)null);
        ReportOrderResult result = await CreateService().ReportOrderAsync(8, 42, new ReportOrderRequestDTO
        {
            Reason = "Sai món"
        });
        Assert.True(result.IsReportValid);
        Assert.False(result.IsOrderFound);
        _orders.VerifyAll();
        _work.VerifyNoOtherCalls();
    }

    private static SalesOrder MakeOrder() => new()

    {

        Id = 42,
        Code = "TEST-ORDER",
        BranchId = 2,
        CreatedByUserId = 7,
        Branch = new Branch

        {

            Code = "CN02",
            Name = "Chi nhánh 2"

        },
        TotalAmount = 54000m,
        Status = "COMPLETED",
        OrderItems = new List<OrderItem>

    {

        new()

    {

        ProductNameSnapshot = "Tên món lúc bán",
        SizeNameSnapshot = "M",
        Quantity = 3,
        UnitPriceSnapshot = 18000m,
        SubtotalAmount = 54000m

    }

    }

    };
}
