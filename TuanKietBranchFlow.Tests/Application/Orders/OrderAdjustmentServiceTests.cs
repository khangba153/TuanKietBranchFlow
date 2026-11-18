using Moq;
using TuanKietBranchFlow.Application.DTOs.Orders.Management;
using TuanKietBranchFlow.Application.Services.Orders;
using TuanKietBranchFlow.Infrastructure.Models;
using TuanKietBranchFlow.Infrastructure.Repositories.Orders;
using TuanKietBranchFlow.Infrastructure.UnitOfWork;

namespace TuanKietBranchFlow.Tests.Application.Orders;

public class OrderAdjustmentServiceTests
{
    private readonly Mock<IOrderManagementService> _management = new Mock<IOrderManagementService>(MockBehavior.Strict);
    private readonly Mock<IOrderMenuRepository> _menu = new Mock<IOrderMenuRepository>(MockBehavior.Strict);
    private readonly Mock<IOrderRepository> _orders = new Mock<IOrderRepository>(MockBehavior.Strict);
    private readonly Mock<IUnitOfWork> _work = new Mock<IUnitOfWork>(MockBehavior.Strict);
    private readonly DateTime _reported = new DateTime(2026, 11, 16, 8, 0, 0);
    private readonly OrderManagementDetailDTO _before;
    private readonly ProductSize _large;
    private OrderAdjustmentService Service() => new(_management.Object, _menu.Object, _orders.Object, _work.Object);
    public OrderAdjustmentServiceTests()
    {
        _before = new()

        {

            Id = 9,
            BranchId = 2,
            Status = "NEEDS_REVIEW",
            ReportedAt = _reported,
            LatestHistoryId = 4,
            Code = "TEST",
            TotalAmount = 69000m,
            ReportReason = "Sai số lượng",
            Items = new()

        {

            new()

        {

            Id = 1,
            ProductId = 1,
            ProductSizeId = 10,
            ProductName = "Tên lúc bán",
            SizeName = "M",
            Quantity = 3,
            UnitPrice = 18000m,
            SubtotalAmount = 69000m,
            Toppings = new()

        {

            new()

        {

            ToppingId = 1,
            Name = "Topping cũ",
            Quantity = 1,
            UnitPrice = 5000m

        }

        },
            Notes = new()

        {

            "Ít đá lúc bán"

        },
            NoteOptionIds = new()

        {

            1

        }

        }

        }

        };
        _large = new()

        {

            Id = 20,
            ProductId = 1,
            Price = 30000,
            Size = new Size

            {

                Name = "L"

            }

        };
        _management.Setup(x => x.GetDetailAsync(7, "ADMIN", 9)).ReturnsAsync(_before);
        _menu.Setup(x => x.GetAvailableCategoriesAsync(2)).ReturnsAsync(new List<Category>
        {
            new()
        {
            Products = new List<Product>
        {
            new()
        {
            Id = 1,
            ProductSizes = new List<ProductSize>
        {
            new()
        {
            Id = 10,
            ProductId = 1,
            Price = 99000,
            Size = new Size
        {
            Name = "M mới"
        }
        },
            _large
        }
        },
            new()
        {
            Id = 2,
            ProductSizes = new List<ProductSize>
        {
            new()
        {
            Id = 30,
            ProductId = 2,
            Price = 10000,
            Size = new Size
        {
            Name = "Khác"
        }
        }
        }
        }
        }
        }
        });
        _menu.Setup(x => x.GetAvailableToppingGroupsAsync(2)).ReturnsAsync(new List<ToppingGroup>
        {
            new()
        {
            Name = "Trân châu",
            Toppings = new List<Topping>
        {
            new()
        {
            Id = 1,
            Name = "Topping hiện tại",
            Price = 9000
        },
            new()
        {
            Id = 2,
            Name = "Mới",
            Price = 7000
        }
        }
        },
            new()
        {
            Name = "Up size",
            Toppings = new List<Topping>
        {
            new()
        {
            Id = 3,
            Name = "Up size",
            Price = 10000
        }
        }
        }
        });
        _menu.Setup(x => x.GetActiveNoteGroupsAsync()).ReturnsAsync(new List<NoteGroup>
        {
            new()
        {
            Id = 1,
            Name = "Đá",
            NoteOptions = new List<NoteOption>
        {
            new()
        {
            Id = 1,
            Name = "Ít đá"
        },
            new()
        {
            Id = 2,
            Name = "Không đá"
        }
        }
        },
            new()
        {
            Id = 2,
            Name = "Đường",
            NoteOptions = new List<NoteOption>
        {
            new()
        {
            Id = 3,
            Name = "Ít đường"
        }
        }
        }
        });
    }

    private AdjustOrderRequestDTO Request() => new()

    {

        Reason = " Đã kiểm tra ",
        ExpectedReportedAt = _reported,
        ExpectedHistoryId = 4,
        Items = new()

    {

        new()

    {

        OrderItemId = 1,
        ProductSizeId = 10,
        Quantity = 2,
        Toppings = new()

    {

        new()

    {

        ToppingId = 1,
        Quantity = 1

    }

    },
        NoteOptionIds = new()

    {

        1

    }

    }

    }

    };
    [Theory]
    [InlineData("OWNER")]
    [InlineData("EMPLOYEE")]
    public async Task NonAdminCannotPreviewOrWrite(string role)
    {
        Assert.True((await Service().PreviewAsync(7, role, 9, Request())).IsForbidden);
        Assert.True((await Service().AdjustAsync(7, role, 9, Request())).IsForbidden);
        _management.VerifyNoOtherCalls();
        _menu.VerifyNoOtherCalls();
        _orders.VerifyNoOtherCalls();
        _work.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SameSizeUsesOriginalPriceAndToppingSnapshot()
    {
        OrderAdjustmentResultDTO result = await Service().PreviewAsync(7, "ADMIN", 9, Request());
        Assert.Null(result.Error);
        Assert.NotNull(result.After);
        Assert.Equal(18000, result.After.Items[0].UnitPrice);
        Assert.Equal(5000, result.After.Items[0].Toppings[0].UnitPrice);
        Assert.Equal("Ít đá lúc bán", result.After.Items[0].Notes[0]);
        Assert.Equal(46000, result.After.TotalAmount);
        Assert.Equal("ADJUSTED", result.After.Status);
        Assert.Equal("NEEDS_REVIEW", _before.Status);
        _orders.VerifyNoOtherCalls();
        _work.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ChangedSizeUsesCurrentSizePriceNotOldOrClientPrice()
    {
        AdjustOrderRequestDTO request = Request();
        request.Items[0].ProductSizeId = 20;
        OrderAdjustmentResultDTO result = await Service().PreviewAsync(7, "ADMIN", 9, request);
        Assert.NotNull(result.After);
        Assert.Equal(30000, result.After.Items[0].UnitPrice);
        Assert.Equal("L", result.After.Items[0].SizeName);
        Assert.Equal(70000, result.After.TotalAmount);
        Assert.Equal("Tên lúc bán", result.After.Items[0].ProductName);
    }

    [Fact]
    public async Task NewToppingUsesCurrentPriceAndNotesCanChange()
    {
        AdjustOrderRequestDTO request = Request();
        request.Items[0].Toppings.Add(new()
        {
            ToppingId = 2,
            Quantity = 2
        });
        request.Items[0].NoteOptionIds = new()

        {

            2,
            3

        };
        OrderAdjustmentResultDTO result = await Service().PreviewAsync(7, "ADMIN", 9, request);
        Assert.NotNull(result.After);
        Assert.Equal(74000, result.After.TotalAmount);
        Assert.Equal(7000, result.After.Items[0].Toppings[1].UnitPrice);
        Assert.Equal(new[]
        {
            "Không đá",
            "Ít đường"
        }, result.After.Items[0].Notes);
    }

    [Fact]
    public async Task DeletedLineIsOmittedButBeforeIsPreserved()
    {
        _before.Items.Add(new()
        {
            Id = 2,
            ProductId = 1,
            ProductSizeId = 10,
            Quantity = 1,
            UnitPrice = 10000,
            SubtotalAmount = 10000
        });
        OrderAdjustmentResultDTO result = await Service().PreviewAsync(7, "ADMIN", 9, Request());
        Assert.NotNull(result.Before);
        Assert.NotNull(result.After);
        Assert.Equal(2, result.Before.Items.Count);
        Assert.Single(result.After.Items);
    }

    [Theory]
    [InlineData(30)]
    [InlineData(999)]
    public async Task SizeFromOtherProductOrUnavailableIsRejected(int size)
    {
        AdjustOrderRequestDTO request = Request();
        request.Items[0].ProductSizeId = size;
        Assert.NotNull((await Service().PreviewAsync(7, "ADMIN", 9, request)).Error);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(1000)]
    public async Task InvalidQuantityIsRejected(int quantity)
    {
        AdjustOrderRequestDTO request = Request();
        request.Items[0].Quantity = quantity;
        Assert.NotNull((await Service().PreviewAsync(7, "ADMIN", 9, request)).Error);
        _work.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task EmptyDuplicateForeignLinesAndNoChangesAreRejected()
    {
        AdjustOrderRequestDTO request = Request();
        request.Items.Clear();
        Assert.NotNull((await Service().PreviewAsync(7, "ADMIN", 9, request)).Error);
        request = Request();
        request.Items.Add(request.Items[0]);
        Assert.NotNull((await Service().PreviewAsync(7, "ADMIN", 9, request)).Error);
        request = Request();
        request.Items[0].OrderItemId = 99;
        Assert.NotNull((await Service().PreviewAsync(7, "ADMIN", 9, request)).Error);
        request = Request();
        request.Items[0].Quantity = 3;
        Assert.NotNull((await Service().PreviewAsync(7, "ADMIN", 9, request)).Error);
        _work.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task MissingAndStaleOrderCannotBeAdjusted()
    {
        _management.Setup(x => x.GetDetailAsync(7, "ADMIN", 9)).ReturnsAsync((OrderManagementDetailDTO?)null);
        Assert.True((await Service().PreviewAsync(7, "ADMIN", 9, Request())).IsNotFound);
        _management.Setup(x => x.GetDetailAsync(7, "ADMIN", 9)).ReturnsAsync(_before);
        _before.LatestHistoryId++;
        Assert.True((await Service().PreviewAsync(7, "ADMIN", 9, Request())).IsConflict);
        _menu.VerifyNoOtherCalls();
        _work.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DuplicateUnavailableAndExcessToppingsAreRejected()
    {
        AdjustOrderRequestDTO request = Request();
        request.Items[0].Toppings.Add(new()
        {
            ToppingId = 1,
            Quantity = 1
        });
        Assert.NotNull((await Service().PreviewAsync(7, "ADMIN", 9, request)).Error);
        request = Request();
        request.Items[0].Toppings[0].ToppingId = 999;
        Assert.NotNull((await Service().PreviewAsync(7, "ADMIN", 9, request)).Error);
        request = Request();
        request.Items[0].Toppings[0].Quantity = 4;
        Assert.NotNull((await Service().PreviewAsync(7, "ADMIN", 9, request)).Error);
        request = Request();
        request.Items[0].Toppings.Add(new()
        {
            ToppingId = 3,
            Quantity = 2
        });
        Assert.NotNull((await Service().PreviewAsync(7, "ADMIN", 9, request)).Error);
    }

    [Fact]
    public async Task TwoNotesSameGroupAndUnavailableNoteAreRejected()
    {
        AdjustOrderRequestDTO request = Request();
        request.Items[0].NoteOptionIds = new()

        {

            1,
            2

        };
        Assert.NotNull((await Service().PreviewAsync(7, "ADMIN", 9, request)).Error);
        request.Items[0].NoteOptionIds = new()

        {

            999

        };
        Assert.NotNull((await Service().PreviewAsync(7, "ADMIN", 9, request)).Error);
    }

    [Fact]
    public async Task ChangedCurrentPriceOrRequestInvalidatesPreview()
    {
        AdjustOrderRequestDTO request = Request();
        request.Items[0].ProductSizeId = 20;
        request.PreviewHash = (await Service().PreviewAsync(7, "ADMIN", 9, request)).PreviewHash;
        _large.Price++;
        Assert.True((await Service().AdjustAsync(7, "ADMIN", 9, request)).IsConflict);
        _large.Price--;
        request.Items[0].Quantity++;
        Assert.True((await Service().AdjustAsync(7, "ADMIN", 9, request)).IsConflict);
        _work.VerifyNoOtherCalls();
        _orders.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ConfirmSavesUpdatedItemsHistoryAndCommitsOnce()
    {
        AdjustOrderRequestDTO request = Request();
        request.PreviewHash = (await Service().PreviewAsync(7, "ADMIN", 9, request)).PreviewHash;
        SetupWrite(1);
        List<OrderItem>? updated = null;
        OrderAdjustment? history = null;
        _orders.Setup(x => x.ApplyAdjustedItemsAsync(9, It.IsAny<List<OrderItem>>(), 46000m)).Callback<int, List<OrderItem>, decimal>((_, items, _) => updated = items).Returns(Task.CompletedTask);
        _orders.Setup(x => x.AddReviewHistoryAsync(It.IsAny<OrderAdjustment>())).Callback<OrderAdjustment>(h => history = h).Returns(Task.CompletedTask);
        _work.Setup(x => x.SaveChangesAsync()).ReturnsAsync(1);
        _work.Setup(x => x.CommitTransactionAsync()).Returns(Task.CompletedTask);
        OrderAdjustmentResultDTO result = await Service().AdjustAsync(7, "ADMIN", 9, request);
        Assert.NotNull(result.After);
        Assert.NotNull(updated);
        Assert.NotNull(history);
        Assert.Equal(2, updated[0].Quantity);
        Assert.Equal(18000m, updated[0].UnitPriceSnapshot);
        OrderHistorySnapshotDTO beforeSnapshot = System.Text.Json.JsonSerializer.Deserialize<OrderHistorySnapshotDTO>(history.BeforeData)!;
        Assert.Equal("Tên lúc bán", beforeSnapshot.Items[0].ProductName);
        Assert.Contains("ADJUSTED", history.AfterData);
        Assert.Equal("Đã kiểm tra", history.Reason);
        _work.Verify(x => x.SaveChangesAsync(), Times.Once);
        _work.Verify(x => x.CommitTransactionAsync(), Times.Once);
    }

    [Fact]
    public async Task ConcurrentLoserDoesNotApplyItemsOrSaveHistory()
    {
        AdjustOrderRequestDTO request = Request();
        request.PreviewHash = (await Service().PreviewAsync(7, "ADMIN", 9, request)).PreviewHash;
        SetupWrite(0);
        _work.Setup(x => x.RollbackTransactionAsync()).Returns(Task.CompletedTask);
        Assert.True((await Service().AdjustAsync(7, "ADMIN", 9, request)).IsConflict);
        _orders.Verify(x => x.ApplyAdjustedItemsAsync(It.IsAny<int>(), It.IsAny<List<OrderItem>>(), It.IsAny<decimal>()), Times.Never);
        _orders.Verify(x => x.AddReviewHistoryAsync(It.IsAny<OrderAdjustment>()), Times.Never);
    }

    [Fact]
    public async Task SaveFailureRollsBackStatusItemsAndHistory()
    {
        AdjustOrderRequestDTO request = Request();
        request.PreviewHash = (await Service().PreviewAsync(7, "ADMIN", 9, request)).PreviewHash;
        SetupWrite(1);
        _orders.Setup(x => x.ApplyAdjustedItemsAsync(9, It.IsAny<List<OrderItem>>(), 46000m)).Returns(Task.CompletedTask);
        _orders.Setup(x => x.AddReviewHistoryAsync(It.IsAny<OrderAdjustment>())).Returns(Task.CompletedTask);
        _work.Setup(x => x.SaveChangesAsync()).ThrowsAsync(new InvalidOperationException("fault"));
        _work.Setup(x => x.RollbackTransactionAsync()).Returns(Task.CompletedTask);
        _work.Setup(x => x.ClearTracking());
        await Assert.ThrowsAsync<InvalidOperationException>(() => Service().AdjustAsync(7, "ADMIN", 9, request));
        _work.Verify(x => x.RollbackTransactionAsync(), Times.Once);
        _work.Verify(x => x.ClearTracking(), Times.Once);
        _work.Verify(x => x.CommitTransactionAsync(), Times.Never);
    }

    private void SetupWrite(int affected)
    {
        _work.Setup(x => x.BeginTransactionAsync()).Returns(Task.CompletedTask);
        _orders.Setup(x => x.TryResolveReviewAsync(9, 7, It.IsAny<DateOnly>(), _reported, 4, "ADJUSTED", It.IsAny<DateTime>())).ReturnsAsync(affected);
    }
}
