using Moq;
using TuanKietBranchFlow.Application.DTOs.Orders.Management;
using TuanKietBranchFlow.Application.Services.Orders;
using TuanKietBranchFlow.Infrastructure.Models;
using TuanKietBranchFlow.Infrastructure.Repositories.Branches;
using TuanKietBranchFlow.Infrastructure.Repositories.Orders;
using Xunit;

namespace TuanKietBranchFlow.Tests.Application.Orders;

public class OrderManagementServiceTests
{
    private readonly Mock<IOrderRepository> _orders = new Mock<IOrderRepository>(MockBehavior.Strict);
    private readonly Mock<IBranchRepository> _branches = new Mock<IBranchRepository>(MockBehavior.Strict);
    private OrderManagementService CreateService() => new(_orders.Object, _branches.Object);
    [Theory]
    [InlineData(1, "EMPLOYEE")]
    [InlineData(1, "")]
    [InlineData(0, "OWNER")]
    public async Task List_InvalidIdentity_DoesNotQueryData(
        int userId,
        string role)
    {
        OrderManagementListResultDTO result = await CreateService().GetOrdersAsync(userId, role, new());
        Assert.True(result.IsForbidden);
        _branches.VerifyNoOtherCalls();
        _orders.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(0, 20, null, null)]
    [InlineData(1000001, 20, null, null)]
    [InlineData(1, 0, null, null)]
    [InlineData(1, 101, null, null)]
    [InlineData(1, 20, 0, null)]
    [InlineData(1, 20, -1, null)]
    [InlineData(1, 20, null, "REPORTED")]
    public async Task List_InvalidQuery_DoesNotQueryData(
        int page,
        int size,
        int? branch,
        string? status)
    {
        OrderManagementListResultDTO result = await CreateService().GetOrdersAsync(1, "OWNER", new()
        {
            Page = page,
            PageSize = size,
            BranchId = branch,
            Status = status
        });
        Assert.False(result.IsValid);
        _branches.VerifyNoOtherCalls();
        _orders.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task List_OverlongKeyword_IsRejected()
    {
        OrderManagementListResultDTO result = await CreateService().GetOrdersAsync(1, "OWNER", new()
        {
            Keyword = new string('x', 101)
        });
        Assert.False(result.IsValid);
        _branches.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task List_Admin_UsesCurrentAssignmentsAndNormalizedFilters()
    {
        DateOnly today = DateOnly.FromDateTime(DateTime.Today);
        DateOnly date = new DateOnly(2026, 11, 1);
        _branches.Setup(x => x.GetAssignedBranchesAsync(8, It.Is<DateOnly>(d => d >= today && d <= today.AddDays(1)))).ReturnsAsync(new List<Branch>
        {
            new()
        {
            Id = 2
        },
            new()
        {
            Id = 4
        },
            new()
        {
            Id = 2
        }
        });
        _orders.Setup(x => x.GetOrdersForManagementAsync(It.Is<List<int>>(ids => ids.SequenceEqual(new[]
        {
            2,
            4
        })), "BF-01", date, "NEEDS_REVIEW", 40, 20)).ReturnsAsync(new OrderManagementPage
        {
            TotalCount = 41,
            Items = new()
        {
            MakeOrder()
        }
        });
        OrderManagementListResultDTO result = await CreateService().GetOrdersAsync(
            8,
            "ADMIN",
            new()
            {
                Keyword = " BF-01 ",
                BusinessDate = date,
                Status = " needs_review ",
                Page = 3
            });
        Assert.True(result.IsValid);
        Assert.False(result.IsForbidden);
        Assert.Equal(41, result.Data.TotalCount);
        Assert.Equal(3, result.Data.Page);
        OrderManagementListItemDTO item = Assert.Single(result.Data.Items);
        Assert.Equal("Nhân viên", item.CreatedByFullName);
        Assert.Equal(3, item.TotalQuantity);
        Assert.Equal(95000m, item.TotalAmount);
        _orders.VerifyAll();
        _branches.VerifyAll();
    }

    [Fact]
    public async Task List_Owner_UsesAllBranchesAndOptionalBranchFilter()
    {
        _branches.Setup(x => x.GetAllNotDeletedAsync()).ReturnsAsync(new List<Branch>
        {
            new()
        {
            Id = 2
        },
            new()
        {
            Id = 4
        }
        });
        _branches.Setup(x => x.GetNotDeletedByIdAsync(4)).ReturnsAsync(new Branch
        {
            Id = 4
        });
        _orders.Setup(x => x.GetOrdersForManagementAsync(It.Is<List<int>>(ids => ids.SequenceEqual(new[]
        {
            4
        })), "", null, "", 0, 20)).ReturnsAsync(new OrderManagementPage());
        OrderManagementListResultDTO result = await CreateService().GetOrdersAsync(1, "OWNER", new()
        {
            BranchId = 4
        });
        Assert.True(result.IsValid);
        Assert.False(result.IsForbidden);
        _orders.VerifyAll();
        _branches.VerifyAll();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task List_BranchOutsideScopeOrMissing_DoesNotQueryOrders(bool exists)
    {
        _branches.Setup(x => x.GetAssignedBranchesAsync(8, It.IsAny<DateOnly>())).ReturnsAsync(new List<Branch>
        {
            new()
        {
            Id = 2
        }
        });
        _branches.Setup(x => x.GetNotDeletedByIdAsync(9)).ReturnsAsync(exists ? new Branch
        {
            Id = 9
        } : null);
        OrderManagementListResultDTO result = await CreateService().GetOrdersAsync(8, "ADMIN", new()
        {
            BranchId = 9
        });
        Assert.Equal(exists, result.IsForbidden);
        Assert.Equal(exists, result.IsBranchFound);
        _orders.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task List_AdminWithoutAssignments_ReturnsEmptyPage()
    {
        _branches.Setup(x => x.GetAssignedBranchesAsync(8, It.IsAny<DateOnly>())).ReturnsAsync(new List<Branch>());
        OrderManagementListResultDTO result = await CreateService().GetOrdersAsync(8, "ADMIN", new());
        Assert.Empty(result.Data.Items);
        Assert.Equal(0, result.Data.TotalCount);
        Assert.Equal(20, result.Data.PageSize);
        Assert.Equal(0, result.Data.Summary.TotalOrders);
        Assert.Equal(0m, result.Data.Summary.CompletedOrderValue);
        _orders.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task List_MapsFullSummaryIndependentlyOfCurrentPageAndStatus()
    {
        _branches.Setup(x => x.GetAllNotDeletedAsync()).ReturnsAsync(new List<Branch>
        {
            new()
        {
            Id = 2
        }
        });
        _orders.Setup(x => x.GetOrdersForManagementAsync(It.Is<List<int>>(ids => ids.SequenceEqual(new[]
        {
            2
        })), "", null, "NEEDS_REVIEW", 20, 20)).ReturnsAsync(new OrderManagementPage
        {
            TotalCount = 21,
            Items = new()
        {
            MakeOrder()
        },
            Summary = new OrderManagementSummary
            {
                TotalOrders = 100,
                CompletedOrders = 60,
                AdjustedOrders = 10,
                NeedsReviewOrders = 21,
                CancelledOrders = 9,
                CompletedOrderValue = 5000000
            }
        });
        OrderManagementListResultDTO result = await CreateService().GetOrdersAsync(1, "OWNER", new()
        {
            Page = 2,
            Status = "NEEDS_REVIEW"
        });
        Assert.Single(result.Data.Items);
        Assert.Equal(21, result.Data.TotalCount);
        Assert.Equal(100, result.Data.Summary.TotalOrders);
        Assert.Equal(60, result.Data.Summary.CompletedOrders);
        Assert.Equal(10, result.Data.Summary.AdjustedOrders);
        Assert.Equal(21, result.Data.Summary.NeedsReviewOrders);
        Assert.Equal(9, result.Data.Summary.CancelledOrders);
        Assert.Equal(5000000m, result.Data.Summary.CompletedOrderValue);
        _orders.VerifyAll();
    }

    [Theory]
    [InlineData(1, "EMPLOYEE", 5)]
    [InlineData(0, "OWNER", 5)]
    [InlineData(1, "OWNER", 0)]
    public async Task Detail_InvalidIdentityOrId_DoesNotQueryData(
        int userId,
        string role,
        int orderId)
    {
        Assert.Null(await CreateService().GetDetailAsync(userId, role, orderId));
        _branches.VerifyNoOtherCalls();
        _orders.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Detail_MissingOrOutsideScope_ReturnsNull(bool outsideScope)
    {
        _branches.Setup(x => x.GetAssignedBranchesAsync(8, It.IsAny<DateOnly>())).ReturnsAsync(new List<Branch>
        {
            new()
        {
            Id = 4
        }
        });
        _orders.Setup(x => x.GetOrderDetailForManagementAsync(5, It.Is<List<int>>(ids => ids.SequenceEqual(new[]
        {
            4
        })))).ReturnsAsync(outsideScope ? MakeOrder() : null);
        Assert.Null(await CreateService().GetDetailAsync(8, "ADMIN", 5));
        _orders.VerifyAll();
    }

    [Fact]
    public async Task Detail_NoAssignments_DoesNotQueryOrders()
    {
        _branches.Setup(x => x.GetAssignedBranchesAsync(8, It.IsAny<DateOnly>())).ReturnsAsync(new List<Branch>());
        Assert.Null(await CreateService().GetDetailAsync(8, "ADMIN", 5));
        _orders.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Detail_Owner_MapsStoredSnapshotsAndReportWithoutCurrentMenu()
    {
        _branches.Setup(x => x.GetAllNotDeletedAsync()).ReturnsAsync(new List<Branch>
        {
            new()
        {
            Id = 2
        }
        });
        _orders.Setup(x => x.GetOrderDetailForManagementAsync(5, It.Is<List<int>>(ids => ids.SequenceEqual(new[]
        {
            2
        })))).ReturnsAsync(MakeOrder());
        OrderManagementDetailDTO? detail = await CreateService().GetDetailAsync(1, "OWNER", 5);
        Assert.NotNull(detail);
        Assert.Equal("BF-01", detail.Code);
        Assert.Equal("CN02", detail.BranchCode);
        Assert.Equal("Nhân viên", detail.CreatedByFullName);
        Assert.Equal(95000m, detail.TotalAmount);
        Assert.Equal(3, detail.TotalQuantity);
        Assert.Equal("Sai món", detail.ReportReason);
        Assert.Equal("Người báo", detail.ReportedByFullName);
        Assert.NotNull(detail.ReportedAt);
        OrderManagementItemDTO first = detail.Items[0];
        Assert.Equal(1, first.Id);
        Assert.Equal("Tên món khi bán", first.ProductName);
        Assert.Equal("M", first.SizeName);
        Assert.Equal(30000m, first.UnitPrice);
        Assert.Equal(70000m, first.SubtotalAmount);
        OrderManagementToppingDTO topping = Assert.Single(first.Toppings);
        Assert.Equal("Trân châu cũ", topping.Name);
        Assert.Equal(1, topping.Quantity);
        Assert.Equal(5000m, topping.UnitPrice);
        Assert.Equal("Ít đá", Assert.Single(first.Notes));
        _orders.VerifyAll();
        _branches.VerifyAll();
    }

    [Fact]
    public async Task Detail_MapsHistoryAndVersionWithoutExposingRawJson()
    {
        SalesOrder order = MakeOrder();
        order.OrderAdjustments.Add(new OrderAdjustment
        {
            Id = 4,
            AdjustedByUser = new AppUser
            {
                FullName = "Quản lý"
            },
            Reason = "Đã kiểm tra",
            CreatedAt = new DateTime(2026, 11, 15),
            BeforeData = "{\"Status\":\"NEEDS_REVIEW\"}",
            AfterData = "{\"Status\":\"COMPLETED\"}"
        });
        order.OrderAdjustments.Add(new OrderAdjustment
        {
            Id = 2,
            AdjustedByUser = new AppUser
            {
                FullName = "Quản lý cũ"
            },
            Reason = "Lịch sử cũ",
            CreatedAt = new DateTime(2026, 11, 14),
            BeforeData = "[]",
            AfterData = "{\"Status\":123}"
        });
        _branches.Setup(x => x.GetAllNotDeletedAsync()).ReturnsAsync(new List<Branch>
        {
            new()
        {
            Id = 2
        }
        });
        _orders.Setup(x => x.GetOrderDetailForManagementAsync(5, It.IsAny<List<int>>())).ReturnsAsync(order);
        OrderManagementDetailDTO? detail = await CreateService().GetDetailAsync(1, "OWNER", 5);
        Assert.NotNull(detail);
        Assert.Equal(4, detail.LatestHistoryId);
        Assert.Equal(2, detail.History.Count);
        Assert.Equal("Quản lý", detail.History[0].ProcessedByFullName);
        Assert.Equal("NEEDS_REVIEW", detail.History[0].BeforeStatus);
        Assert.Equal("COMPLETED", detail.History[0].AfterStatus);
        Assert.Null(detail.History[1].BeforeStatus);
        Assert.Null(detail.History[1].AfterStatus);
    }

    // Navigation menu cố ý không gắn: đọc lịch sử phải dựa trên snapshot đã lưu.
    private static SalesOrder MakeOrder() => new()

    {

        Id = 5,
        Code = "BF-01",
        BranchId = 2,
        Branch = new Branch

        {

            Id = 2,
            Code = "CN02",
            Name = "Chi nhánh 2"

        },
        CreatedByUserId = 3,
        CreatedByUser = new AppUser

        {

            FullName = "Nhân viên"

        },
        BusinessDate = new DateOnly(2026, 11, 1),
        CreatedAt = new DateTime(2026, 11, 1, 8, 0, 0),
        TotalAmount = 95000m,
        Status = "NEEDS_REVIEW",
        ReportReason = "Sai món",
        ReportedAt = new DateTime(2026, 11, 1, 9, 0, 0),
        ReportedByUser = new AppUser

        {

            FullName = "Người báo"

        },
        OrderItems = new List<OrderItem>

    {

        new()

    {

        Id = 2,
        ProductNameSnapshot = "Món khác",
        SizeNameSnapshot = "L",
        Quantity = 1,
        UnitPriceSnapshot = 25000,
        SubtotalAmount = 25000

    },
        new()

    {

        Id = 1,
        ProductNameSnapshot = "Tên món khi bán",
        SizeNameSnapshot = "M",
        Quantity = 2,
        UnitPriceSnapshot = 30000,
        SubtotalAmount = 70000,
        OrderItemToppings = new List<OrderItemTopping>

    {

        new()

    {

        ToppingNameSnapshot = "Trân châu cũ",
        Quantity = 1,
        UnitPriceSnapshot = 5000

    }

    },
        OrderItemNotes = new List<OrderItemNote>

    {

        new()

    {

        NoteNameSnapshot = "Ít đá"

    }

    }

    }

    }

    };
}
