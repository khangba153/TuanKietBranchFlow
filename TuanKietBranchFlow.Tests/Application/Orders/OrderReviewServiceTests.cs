using System.Text.Json;
using Moq;
using TuanKietBranchFlow.Application.DTOs.Orders.Management;
using TuanKietBranchFlow.Application.Services.Orders;
using TuanKietBranchFlow.Infrastructure.Models;
using TuanKietBranchFlow.Infrastructure.Repositories.Orders;
using TuanKietBranchFlow.Infrastructure.UnitOfWork;

namespace TuanKietBranchFlow.Tests.Application.Orders;

public class OrderReviewServiceTests
{
    private readonly Mock<IOrderManagementService> _management = new Mock<IOrderManagementService>(MockBehavior.Strict);
    private readonly Mock<IOrderRepository> _orders = new Mock<IOrderRepository>(MockBehavior.Strict);
    private readonly Mock<IUnitOfWork> _work = new Mock<IUnitOfWork>(MockBehavior.Strict);
    private readonly DateTime _reportedAt = new DateTime(2026, 11, 15, 8, 0, 0);
    private OrderReviewService Service() => new(_management.Object, _orders.Object, _work.Object);
    private ResolveOrderReviewRequestDTO Request(string action = "KEEP") => new()

    {

        Action = action,
        Reason = "  Đã kiểm tra với khách  ",
        ExpectedReportedAt = _reportedAt,
        ExpectedHistoryId = 4

    };
    private OrderManagementDetailDTO Detail() => new()

    {

        Id = 9,
        Code = "CN02-TEST",
        BranchId = 2,
        Status = "NEEDS_REVIEW",
        TotalAmount = 54000,
        ReportedAt = _reportedAt,
        ReportReason = "Nghi sai size",
        LatestHistoryId = 4,
        Items = new()

    {

        new()

    {

        ProductName = "Tên snapshot",
        SizeName = "M",
        Quantity = 3,
        UnitPrice = 18000,
        SubtotalAmount = 54000

    }

    }

    };
    [Theory]
    [InlineData(1, "OWNER")]
    [InlineData(1, "EMPLOYEE")]
    [InlineData(0, "ADMIN")]
    public async Task NonAdmin_IsForbiddenWithoutQueries(
        int userId,
        string role)
    {
        Assert.True((await Service().ResolveAsync(userId, role, 9, Request())).IsForbidden);
        _management.VerifyNoOtherCalls();
        _orders.VerifyNoOtherCalls();
        _work.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("OTHER", "Reason")]
    [InlineData("KEEP", " ")]
    [InlineData("CANCEL", "")]
    public async Task InvalidActionOrReason_DoesNotBeginTransaction(
        string action,
        string reason)
    {
        ResolveOrderReviewRequestDTO request = Request(action);
        request.Reason = reason;
        Assert.True((await Service().ResolveAsync(1, "ADMIN", 9, request)).IsInvalid);
        _management.VerifyNoOtherCalls();
        _work.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task InvalidVersionOrLength_IsRejected()
    {
        ResolveOrderReviewRequestDTO request = Request();
        request.ExpectedReportedAt = null;
        Assert.True((await Service().ResolveAsync(1, "ADMIN", 9, request)).IsInvalid);
        request = Request();
        request.ExpectedHistoryId = -1;
        Assert.True((await Service().ResolveAsync(1, "ADMIN", 9, request)).IsInvalid);
        request = Request();
        request.Reason = new string('a', 501);
        Assert.True((await Service().ResolveAsync(1, "ADMIN", 9, request)).IsInvalid);
        _management.VerifyNoOtherCalls();
        _work.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task OutsideScope_IsNotFoundWithoutWriting()
    {
        _management.Setup(x => x.GetDetailAsync(1, "ADMIN", 9)).ReturnsAsync((OrderManagementDetailDTO?)null);
        Assert.True((await Service().ResolveAsync(1, "ADMIN", 9, Request())).IsNotFound);
        _orders.VerifyNoOtherCalls();
        _work.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("COMPLETED", 4, false)]
    [InlineData("CANCELLED", 4, false)]
    [InlineData("NEEDS_REVIEW", 5, false)]
    [InlineData("NEEDS_REVIEW", 4, true)]
    public async Task StaleStatusOrReviewCycle_IsConflict(
        string status,
        int historyId,
        bool newReport)
    {
        OrderManagementDetailDTO detail = Detail();
        detail.Status = status;
        detail.LatestHistoryId = historyId;
        if (newReport)
        {
            detail.ReportedAt = _reportedAt.AddSeconds(1);
        }

        _management.Setup(x => x.GetDetailAsync(1, "ADMIN", 9)).ReturnsAsync(detail);
        Assert.True((await Service().ResolveAsync(1, "ADMIN", 9, Request())).IsConflict);
        _orders.VerifyNoOtherCalls();
        _work.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("KEEP", "COMPLETED")]
    [InlineData("CANCEL", "CANCELLED")]
    public async Task ValidResolution_SavesSnapshotsAndCommitsOnce(
        string action,
        string target)
    {
        OrderManagementDetailDTO detail = Detail();
        _management.Setup(x => x.GetDetailAsync(1, "ADMIN", 9)).ReturnsAsync(detail);
        _work.Setup(x => x.BeginTransactionAsync()).Returns(Task.CompletedTask);
        _orders.Setup(x => x.TryResolveReviewAsync(9, 1, It.IsAny<DateOnly>(), _reportedAt, 4, target, It.IsAny<DateTime>())).ReturnsAsync(1);
        OrderAdjustment? saved = null;
        _orders.Setup(x => x.AddReviewHistoryAsync(It.IsAny<OrderAdjustment>())).Callback<OrderAdjustment>(x => saved = x).Returns(Task.CompletedTask);
        _work.Setup(x => x.SaveChangesAsync()).ReturnsAsync(1);
        _work.Setup(x => x.CommitTransactionAsync()).Returns(Task.CompletedTask);
        OrderReviewResultDTO result = await Service().ResolveAsync(1, "ADMIN", 9, Request(action));
        Assert.Equal(target, result.Status);
        Assert.NotNull(saved);
        Assert.Equal("Đã kiểm tra với khách", saved.Reason);
        Assert.Equal(1, saved.AdjustedByUserId);
        Assert.Equal(9, saved.SalesOrderId);
        using var before = JsonDocument.Parse(saved.BeforeData);
        using var after = JsonDocument.Parse(saved.AfterData);
        Assert.Equal("NEEDS_REVIEW", before.RootElement.GetProperty("Status").GetString());
        Assert.Equal(target, after.RootElement.GetProperty("Status").GetString());
        Assert.Equal(54000, after.RootElement.GetProperty("TotalAmount").GetDecimal());
        Assert.Equal("Nghi sai size", after.RootElement.GetProperty("ReportReason").GetString());
        Assert.Equal(
            before.RootElement.GetProperty("Items").GetRawText(),
            after.RootElement.GetProperty("Items").GetRawText());
        Assert.False(after.RootElement.TryGetProperty("History", out _));
        _work.Verify(x => x.SaveChangesAsync(), Times.Once);
        _work.Verify(x => x.CommitTransactionAsync(), Times.Once);
        _work.Verify(x => x.RollbackTransactionAsync(), Times.Never);
    }

    [Fact]
    public async Task ConcurrentResolutionOrRevokedAssignment_RollsBackWithoutHistory()
    {
        _management.Setup(x => x.GetDetailAsync(1, "ADMIN", 9)).ReturnsAsync(Detail());
        _work.Setup(x => x.BeginTransactionAsync()).Returns(Task.CompletedTask);
        _orders.Setup(x => x.TryResolveReviewAsync(9, 1, It.IsAny<DateOnly>(), _reportedAt, 4, "COMPLETED", It.IsAny<DateTime>())).ReturnsAsync(0);
        _work.Setup(x => x.RollbackTransactionAsync()).Returns(Task.CompletedTask);
        Assert.True((await Service().ResolveAsync(1, "ADMIN", 9, Request())).IsConflict);
        _orders.Verify(x => x.AddReviewHistoryAsync(It.IsAny<OrderAdjustment>()), Times.Never);
        _work.Verify(x => x.SaveChangesAsync(), Times.Never);
        _work.Verify(x => x.CommitTransactionAsync(), Times.Never);
    }

    [Fact]
    public async Task HistorySaveFailure_RollsBackAndClearsTracking()
    {
        _management.Setup(x => x.GetDetailAsync(1, "ADMIN", 9)).ReturnsAsync(Detail());
        _work.Setup(x => x.BeginTransactionAsync()).Returns(Task.CompletedTask);
        _orders.Setup(x => x.TryResolveReviewAsync(9, 1, It.IsAny<DateOnly>(), _reportedAt, 4, "COMPLETED", It.IsAny<DateTime>())).ReturnsAsync(1);
        _orders.Setup(x => x.AddReviewHistoryAsync(It.IsAny<OrderAdjustment>())).Returns(Task.CompletedTask);
        _work.Setup(x => x.SaveChangesAsync()).ThrowsAsync(new InvalidOperationException("Simulated database failure"));
        _work.Setup(x => x.RollbackTransactionAsync()).Returns(Task.CompletedTask);
        _work.Setup(x => x.ClearTracking());
        await Assert.ThrowsAsync<InvalidOperationException>(() => Service().ResolveAsync(1, "ADMIN", 9, Request()));
        _work.Verify(x => x.RollbackTransactionAsync(), Times.Once);
        _work.Verify(x => x.ClearTracking(), Times.Once);
        _work.Verify(x => x.CommitTransactionAsync(), Times.Never);
    }
}
