using System.Text.Json;
using Moq;
using TuanKietBranchFlow.Application.DTOs.Orders;
using TuanKietBranchFlow.Application.DTOs.Orders.Management;
using TuanKietBranchFlow.Application.Services.Orders;
using TuanKietBranchFlow.Infrastructure.Models;
using TuanKietBranchFlow.Infrastructure.Repositories.Branches;
using TuanKietBranchFlow.Infrastructure.Repositories.Orders;
using TuanKietBranchFlow.Infrastructure.UnitOfWork;

namespace TuanKietBranchFlow.Tests.Application.Orders;

public class EmployeeOrderResolutionTests
{
    private readonly Mock<IOrderRepository> _orderRepositoryMock =
        new Mock<IOrderRepository>(MockBehavior.Strict);

    private readonly Mock<IBranchRepository> _branchRepositoryMock =
        new Mock<IBranchRepository>(MockBehavior.Strict);

    private readonly Mock<IUnitOfWork> _unitOfWorkMock =
        new Mock<IUnitOfWork>(MockBehavior.Strict);

    private OrderService CreateService()
    {
        return new OrderService(
            _branchRepositoryMock.Object,
            _orderRepositoryMock.Object,
            _unitOfWorkMock.Object);
    }

    [Theory]
    [InlineData("COMPLETED")]
    [InlineData("ADJUSTED")]
    public async Task ReportAgain_ReviewedLatestResolution_UsesConditionalUpdate(string status)
    {
        SalesOrder order = CreateOrder(status);
        _orderRepositoryMock.Setup(repository => repository.GetOrderForReportAsync(42, 7))
            .ReturnsAsync(order);
        _orderRepositoryMock.Setup(repository => repository.TryReportOrderAsync(
            42, 7, 50, "Vẫn thiếu topping", It.IsAny<DateTime>()))
            .ReturnsAsync(1);

        ReportOrderResult result = await CreateService().ReportOrderAsync(
            7, 42, new ReportOrderRequestDTO
            {
                Reason = "  Vẫn thiếu topping  ",
                ExpectedHistoryId = 50,
                HasReviewedResolution = true
            });

        Assert.True(result.CanReport);
        Assert.Equal("NEEDS_REVIEW", result.Order?.Status);
        Assert.Equal("Vẫn thiếu topping", result.Order?.ReportReason);
        Assert.Single(order.OrderAdjustments);
        _orderRepositoryMock.VerifyAll();
        _unitOfWorkMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ReportAgain_WithoutAcknowledgment_DoesNotWrite()
    {
        _orderRepositoryMock.Setup(repository => repository.GetOrderForReportAsync(42, 7))
            .ReturnsAsync(CreateOrder("ADJUSTED"));

        ReportOrderResult result = await CreateService().ReportOrderAsync(
            7, 42, new ReportOrderRequestDTO
            {
                Reason = "Sai size",
                ExpectedHistoryId = 50
            });

        Assert.False(result.IsReportValid);
        Assert.False(result.CanReport);
        Assert.Contains("xác nhận", result.ErrorMessage);
        _orderRepositoryMock.VerifyAll();
        _orderRepositoryMock.VerifyNoOtherCalls();
        _unitOfWorkMock.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(49)]
    [InlineData(51)]
    public async Task ReportAgain_StaleOrForgedVersion_ReturnsConflictWithoutWriting(int historyId)
    {
        _orderRepositoryMock.Setup(repository => repository.GetOrderForReportAsync(42, 7))
            .ReturnsAsync(CreateOrder("COMPLETED"));

        ReportOrderResult result = await CreateService().ReportOrderAsync(
            7, 42, new ReportOrderRequestDTO
            {
                Reason = "Sai size",
                ExpectedHistoryId = historyId,
                HasReviewedResolution = true
            });

        Assert.True(result.IsReportValid);
        Assert.True(result.IsOrderFound);
        Assert.False(result.CanReport);
        _orderRepositoryMock.VerifyAll();
        _orderRepositoryMock.VerifyNoOtherCalls();
        _unitOfWorkMock.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("NEEDS_REVIEW")]
    [InlineData("CANCELLED")]
    public async Task ReportAgain_NonReportableStatus_DoesNotWrite(string status)
    {
        _orderRepositoryMock.Setup(repository => repository.GetOrderForReportAsync(42, 7))
            .ReturnsAsync(CreateOrder(status));

        ReportOrderResult result = await CreateService().ReportOrderAsync(
            7, 42, new ReportOrderRequestDTO
            {
                Reason = "Sai size",
                ExpectedHistoryId = 50,
                HasReviewedResolution = true
            });

        Assert.False(result.CanReport);
        _orderRepositoryMock.VerifyAll();
        _orderRepositoryMock.VerifyNoOtherCalls();
        _unitOfWorkMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ReportAgain_OrderChangesBeforeWrite_ReturnsConflict()
    {
        _orderRepositoryMock.Setup(repository => repository.GetOrderForReportAsync(42, 7))
            .ReturnsAsync(CreateOrder("ADJUSTED"));
        _orderRepositoryMock.Setup(repository => repository.TryReportOrderAsync(
            42, 7, 50, "Sai size", It.IsAny<DateTime>()))
            .ReturnsAsync(0);

        ReportOrderResult result = await CreateService().ReportOrderAsync(
            7, 42, new ReportOrderRequestDTO
            {
                Reason = "Sai size",
                ExpectedHistoryId = 50,
                HasReviewedResolution = true
            });

        Assert.False(result.CanReport);
        Assert.Null(result.Order);
        _unitOfWorkMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Detail_ReturnsResolutionReasonAndStoredBeforeAfterItems()
    {
        SalesOrder order = CreateOrder("ADJUSTED");
        OrderAdjustment history = Assert.Single(order.OrderAdjustments);
        history.BeforeData = CreateSnapshot("NEEDS_REVIEW", "M", 20000);
        history.AfterData = CreateSnapshot("ADJUSTED", "L", 25000);
        _orderRepositoryMock.Setup(repository => repository.GetOrderDetailCreatedByUserAsync(42, 7))
            .ReturnsAsync(order);

        MyOrderDetailDTO? detail = await CreateService().GetMyOrderDetailAsync(7, 42);

        Assert.NotNull(detail);
        Assert.Equal(50, detail.LatestHistoryId);
        MyOrderResolutionDTO resolution = Assert.Single(detail.History);
        Assert.Equal("Đã đổi size theo yêu cầu", resolution.Reason);
        Assert.Equal("Quản lý thử nghiệm", resolution.ProcessedByFullName);
        Assert.Equal("M", Assert.Single(resolution.BeforeItems).SizeName);
        Assert.Equal("L", Assert.Single(resolution.AfterItems).SizeName);
        Assert.Equal(25000, resolution.AfterTotalAmount);
        _unitOfWorkMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Detail_UnreadableLegacySnapshot_StillReturnsResolution()
    {
        SalesOrder order = CreateOrder("COMPLETED");
        OrderAdjustment history = Assert.Single(order.OrderAdjustments);
        history.BeforeData = "not-json";
        history.AfterData = "null";
        _orderRepositoryMock.Setup(repository => repository.GetOrderDetailCreatedByUserAsync(42, 7))
            .ReturnsAsync(order);

        MyOrderDetailDTO? detail = await CreateService().GetMyOrderDetailAsync(7, 42);

        Assert.NotNull(detail);
        MyOrderResolutionDTO resolution = Assert.Single(detail.History);
        Assert.Equal(history.Reason, resolution.Reason);
        Assert.Empty(resolution.BeforeItems);
        Assert.Empty(resolution.AfterItems);
    }

    private static SalesOrder CreateOrder(string status)
    {
        return new SalesOrder
        {
            Id = 42,
            Code = "RESOLUTION-TEST",
            CreatedByUserId = 7,
            Status = status,
            Branch = new Branch
            {
                Code = "CN02",
                Name = "Chi nhánh thử nghiệm"
            },
            OrderAdjustments = new List<OrderAdjustment>
            {
                new OrderAdjustment
                {
                    Id = 50,
                    Reason = "Đã đổi size theo yêu cầu",
                    CreatedAt = new DateTime(2026, 11, 16, 10, 0, 0, DateTimeKind.Utc),
                    AdjustedByUser = new AppUser
                    {
                        FullName = "Quản lý thử nghiệm"
                    },
                    BeforeData = "{}",
                    AfterData = "{}"
                }
            }
        };
    }

    private static string CreateSnapshot(string status, string size, decimal total)
    {
        return JsonSerializer.Serialize(new OrderHistorySnapshotDTO
        {
            Status = status,
            TotalAmount = total,
            Items = new List<OrderManagementItemDTO>
            {
                new OrderManagementItemDTO
                {
                    ProductName = "Tên lúc bán",
                    SizeName = size,
                    Quantity = 1,
                    UnitPrice = total,
                    SubtotalAmount = total
                }
            }
        });
    }
}
