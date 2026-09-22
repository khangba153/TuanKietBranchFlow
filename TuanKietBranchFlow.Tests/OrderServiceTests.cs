using Moq;
using TuanKietBranchFlow.Application.DTOs.Orders;
using TuanKietBranchFlow.Application.Services;
using TuanKietBranchFlow.Infrastructure.Models;
using TuanKietBranchFlow.Infrastructure.Repositories;
using TuanKietBranchFlow.Infrastructure.UnitOfWork;

namespace TuanKietBranchFlow.Tests;

public class OrderServiceTests
{
    // Chi nhánh không tồn tại thì không kiểm tra phân công
    // không đọc dữ liệu món và không mở transaction
    [Fact]
    public async Task CreateOrderAsync_BranchNotFound_ReturnsNotFoundWithoutSaving()
    {
        // Arrange: thay dependency thật bằng mock
        Mock<IBranchRepository> branchRepositoryMock =
            new Mock<IBranchRepository>();

        Mock<IOrderRepository> orderRepositoryMock =
            new Mock<IOrderRepository>();

        Mock<IUnitOfWork> unitOfWorkMock =
            new Mock<IUnitOfWork>();

        OrderService orderService = new OrderService(
            branchRepositoryMock.Object,
            orderRepositoryMock.Object,
            unitOfWorkMock.Object);

        int currentUserId = 10;
        int branchId = 999;

        CreateOrderRequestDTO request = new CreateOrderRequestDTO
        {
            BranchId = branchId,
            Items = new List<CreateOrderItemRequestDTO>
            {
                new CreateOrderItemRequestDTO
                {
                    ProductSizeId = 1,
                    Quantity = 1
                }
            }
        };

        branchRepositoryMock
            .Setup(repository =>
                repository.GetNotDeletedByIdAsync(branchId))
            .ReturnsAsync((Branch?)null);

        // Act: gọi method thật của Service
        CreateOrderResultDTO result =
            await orderService.CreateOrderAsync(
                currentUserId, request);

        // Assert: trả đúng trạng thái chi nhánh không tồn tại
        Assert.False(result.IsBranchFound);
        Assert.False(result.HasAccess);
        Assert.False(result.IsOrderValid);
        Assert.Null(result.Order);
        Assert.False(string.IsNullOrWhiteSpace(result.ErrorMessage));

        // Không tìm thấy Branch thì dừng ngay
        branchRepositoryMock.Verify(
            repository =>
                repository.HasActiveAssignmentAsync(
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<DateOnly>()), Times.Never);

        orderRepositoryMock.VerifyNoOtherCalls();
        unitOfWorkMock.VerifyNoOtherCalls();
    }

    // Chi nhánh tồn tại nhưng employee không có phân công
    // thì không được đọc dữ liệu món hoặc mở transaction
    [Fact]
    public async Task CreateOrderAsync_WithoutAssignment_ReturnsForbiddenWithoutSaving()
    {
        // Arrange
        Mock<IBranchRepository> branchRepositoryMock =
            new Mock<IBranchRepository>();

        Mock<IOrderRepository> orderRepositoryMock =
            new Mock<IOrderRepository>();

        Mock<IUnitOfWork> unitOfWorkMock =
            new Mock<IUnitOfWork>();

        OrderService orderService = new OrderService(
            branchRepositoryMock.Object,
            orderRepositoryMock.Object,
            unitOfWorkMock.Object);

        int currentUserId = 10;
        int branchId = 1;

        Branch branch = new Branch
        {
            Id = branchId,
            Code = "CN01",
            Name = "Chi nhánh trung tâm",
            Address = "Địa chỉ kiểm thử",
            IsActive = true,
            Deleted = false
        };

        CreateOrderRequestDTO request = new CreateOrderRequestDTO
        {
            BranchId = branchId,
            Items = new List<CreateOrderItemRequestDTO>
            {
                new CreateOrderItemRequestDTO
                {
                    ProductSizeId = 1,
                    Quantity = 1
                }
            }
        };

        branchRepositoryMock
            .Setup(repository =>
                repository.GetNotDeletedByIdAsync(branchId))
            .ReturnsAsync(branch);

        branchRepositoryMock
            .Setup(repository =>
                repository.HasActiveAssignmentAsync(
                    currentUserId,
                    branchId,
                    It.IsAny<DateOnly>()))
            .ReturnsAsync(false);

        // Act
        CreateOrderResultDTO result =
            await orderService.CreateOrderAsync(
                currentUserId, request);

        // Assert
        Assert.True(result.IsBranchFound);
        Assert.False(result.HasAccess);
        Assert.False(result.IsOrderValid);
        Assert.Null(result.Order);
        Assert.False(string.IsNullOrWhiteSpace(result.ErrorMessage));

        // Chi nhánh tồn tại nên Service phải kiểm tra phân công đúng 1 lần
        branchRepositoryMock.Verify(
            repository =>
                repository.HasActiveAssignmentAsync(
                    currentUserId,
                    branchId,
                    It.IsAny<DateOnly>()), Times.Once);

        // Không có quyền thì không đọc dữ liệu order, cấp mã hoặc lưu đơn.
        orderRepositoryMock.VerifyNoOtherCalls();
        unitOfWorkMock.VerifyNoOtherCalls();
    }

    // ProductSize không tồn tại hoặc không bán tại chi nhánh
    // thì phải bị từ chối trước khi cấp mã và mở transaction.
    [Fact]
    public async Task CreateOrderAsync_InvalidProductSize_ReturnsInvalidWithoutSaving()
    {
        // Arrange
        Mock<IBranchRepository> branchRepositoryMock =
            new Mock<IBranchRepository>();

        Mock<IOrderRepository> orderRepositoryMock =
            new Mock<IOrderRepository>();

        Mock<IUnitOfWork> unitOfWorkMock =
            new Mock<IUnitOfWork>();

        OrderService orderService = new OrderService(
            branchRepositoryMock.Object,
            orderRepositoryMock.Object,
            unitOfWorkMock.Object);

        int currentUserId = 10;
        int branchId = 1;
        int invalidProductSizeId = 999;

        Branch branch = new Branch
        {
            Id = branchId,
            Code = "CN01",
            Name = "Chi nhánh trung tâm",
            Address = "Địa chỉ kiểm thử",
            IsActive = true,
            Deleted = false
        };

        CreateOrderRequestDTO request = new CreateOrderRequestDTO
        {
            BranchId = branchId,
            Items = new List<CreateOrderItemRequestDTO>
        {
            new CreateOrderItemRequestDTO
            {
                ProductSizeId = invalidProductSizeId,
                Quantity = 1
            }
        }
        };

        branchRepositoryMock
            .Setup(repository =>
                repository.GetNotDeletedByIdAsync(branchId))
            .ReturnsAsync(branch);

        branchRepositoryMock
            .Setup(repository =>
                repository.HasActiveAssignmentAsync(
                    currentUserId,
                    branchId,
                    It.IsAny<DateOnly>()))
            .ReturnsAsync(true);

        // Repository mô phỏng việc bỏ qua ProductSizeId không hợp lệ.
        orderRepositoryMock
            .Setup(repository =>
                repository.GetAvailableProductSizesAsync(
                    branchId,
                    It.IsAny<List<int>>()))
            .ReturnsAsync(new List<ProductSize>());

        // Act
        CreateOrderResultDTO result =
            await orderService.CreateOrderAsync(
                currentUserId,
                request);

        // Assert
        Assert.True(result.IsBranchFound);
        Assert.True(result.HasAccess);
        Assert.False(result.IsOrderValid);
        Assert.Null(result.Order);
        Assert.False(string.IsNullOrWhiteSpace(result.ErrorMessage));

        // Service phải gửi đúng ID duy nhất xuống repository.
        orderRepositoryMock.Verify(
            repository =>
                repository.GetAvailableProductSizesAsync(
                    branchId,
                    It.Is<List<int>>(ids =>
                        ids.Count == 1
                        && ids[0] == invalidProductSizeId)),
            Times.Once);

        // Request sai phải dừng trước khi cấp mã và lưu.
        orderRepositoryMock.Verify(
            repository =>
                repository.GetNextOrderCodeAsync(
                    It.IsAny<int>(),
                    It.IsAny<DateOnly>()),
            Times.Never);

        orderRepositoryMock.Verify(
            repository =>
                repository.AddAsync(It.IsAny<SalesOrder>()),
            Times.Never);

        unitOfWorkMock.VerifyNoOtherCalls();
    }

    // Topping không tồn tại hoặc không bán tại chi nhánh
    // thì phải bị từ chối trước khi cấp mã và mở transaction.
    [Fact]
    public async Task CreateOrderAsync_InvalidTopping_ReturnsInvalidWithoutSaving()
    {
        // Arrange
        Mock<IBranchRepository> branchRepositoryMock =
            new Mock<IBranchRepository>();

        Mock<IOrderRepository> orderRepositoryMock =
            new Mock<IOrderRepository>();

        Mock<IUnitOfWork> unitOfWorkMock =
            new Mock<IUnitOfWork>();

        OrderService orderService = new OrderService(
            branchRepositoryMock.Object,
            orderRepositoryMock.Object,
            unitOfWorkMock.Object);

        int currentUserId = 10;
        int branchId = 1;
        int productSizeId = 10;
        int invalidToppingId = 999;

        Branch branch = new Branch
        {
            Id = branchId,
            Code = "CN01",
            Name = "Chi nhánh trung tâm",
            Address = "Địa chỉ kiểm thử",
            IsActive = true,
            Deleted = false
        };

        CreateOrderRequestDTO request = new CreateOrderRequestDTO
        {
            BranchId = branchId,
            Items = new List<CreateOrderItemRequestDTO>
        {
            new CreateOrderItemRequestDTO
            {
                ProductSizeId = productSizeId,
                Quantity = 1,
                Toppings = new List<CreateOrderItemToppingRequestDTO>
                {
                    new CreateOrderItemToppingRequestDTO
                    {
                        ToppingId = invalidToppingId,
                        Quantity = 1
                    }
                }
            }
        }
        };

        branchRepositoryMock
            .Setup(repository =>
                repository.GetNotDeletedByIdAsync(branchId))
            .ReturnsAsync(branch);

        branchRepositoryMock
            .Setup(repository =>
                repository.HasActiveAssignmentAsync(
                    currentUserId,
                    branchId,
                    It.IsAny<DateOnly>()))
            .ReturnsAsync(true);

        // ProductSize hợp lệ để Service đi tiếp đến bước kiểm tra topping.
        orderRepositoryMock
            .Setup(repository =>
                repository.GetAvailableProductSizesAsync(
                    branchId,
                    It.IsAny<List<int>>()))
            .ReturnsAsync(new List<ProductSize>
            {
            new ProductSize
            {
                Id = productSizeId
            }
            });

        // Repository mô phỏng việc bỏ qua ToppingId không hợp lệ.
        orderRepositoryMock
            .Setup(repository =>
                repository.GetAvailableToppingsAsync(
                    branchId,
                    It.IsAny<List<int>>()))
            .ReturnsAsync(new List<Topping>());

        // Act
        CreateOrderResultDTO result =
            await orderService.CreateOrderAsync(
                currentUserId,
                request);

        // Assert
        Assert.True(result.IsBranchFound);
        Assert.True(result.HasAccess);
        Assert.False(result.IsOrderValid);
        Assert.Null(result.Order);
        Assert.False(string.IsNullOrWhiteSpace(result.ErrorMessage));

        orderRepositoryMock.Verify(
            repository =>
                repository.GetAvailableProductSizesAsync(
                    branchId,
                    It.Is<List<int>>(ids =>
                        ids.Count == 1
                        && ids[0] == productSizeId)),
            Times.Once);

        orderRepositoryMock.Verify(
            repository =>
                repository.GetAvailableToppingsAsync(
                    branchId,
                    It.Is<List<int>>(ids =>
                        ids.Count == 1
                        && ids[0] == invalidToppingId)),
            Times.Once);

        // Request sai phải dừng trước khi cấp mã và lưu.
        orderRepositoryMock.Verify(
            repository =>
                repository.GetNextOrderCodeAsync(
                    It.IsAny<int>(),
                    It.IsAny<DateOnly>()),
            Times.Never);

        orderRepositoryMock.Verify(
            repository =>
                repository.AddAsync(It.IsAny<SalesOrder>()),
            Times.Never);

        unitOfWorkMock.VerifyNoOtherCalls();
    }
    // NoteOption không tồn tại hoặc không còn hoạt động
    // thì phải bị từ chối trước khi cấp mã và mở transaction.
    [Fact]
    public async Task CreateOrderAsync_InvalidNoteOption_ReturnsInvalidWithoutSaving()
    {
        // Arrange
        Mock<IBranchRepository> branchRepositoryMock =
            new Mock<IBranchRepository>();

        Mock<IOrderRepository> orderRepositoryMock =
            new Mock<IOrderRepository>();

        Mock<IUnitOfWork> unitOfWorkMock =
            new Mock<IUnitOfWork>();

        OrderService orderService = new OrderService(
            branchRepositoryMock.Object,
            orderRepositoryMock.Object,
            unitOfWorkMock.Object);

        int currentUserId = 10;
        int branchId = 1;
        int productSizeId = 10;
        int invalidNoteOptionId = 999;

        Branch branch = new Branch
        {
            Id = branchId,
            Code = "CN01",
            Name = "Chi nhánh trung tâm",
            Address = "Địa chỉ kiểm thử",
            IsActive = true,
            Deleted = false
        };

        CreateOrderRequestDTO request = new CreateOrderRequestDTO
        {
            BranchId = branchId,
            Items = new List<CreateOrderItemRequestDTO>
        {
            new CreateOrderItemRequestDTO
            {
                ProductSizeId = productSizeId,
                Quantity = 1,
                NoteOptionIds = new List<int>
                {
                    invalidNoteOptionId
                }
            }
        }
        };

        branchRepositoryMock
            .Setup(repository =>
                repository.GetNotDeletedByIdAsync(branchId))
            .ReturnsAsync(branch);

        branchRepositoryMock
            .Setup(repository =>
                repository.HasActiveAssignmentAsync(
                    currentUserId,
                    branchId,
                    It.IsAny<DateOnly>()))
            .ReturnsAsync(true);

        // ProductSize hợp lệ để Service đi đến bước kiểm tra ghi chú.
        orderRepositoryMock
            .Setup(repository =>
                repository.GetAvailableProductSizesAsync(
                    branchId,
                    It.IsAny<List<int>>()))
            .ReturnsAsync(new List<ProductSize>
            {
                new ProductSize
                {
                    Id = productSizeId
                }
            });

        // Repository không tìm thấy NoteOption hợp lệ tương ứng.
        orderRepositoryMock
            .Setup(repository =>
                repository.GetActiveNoteOptionsAsync(
                    It.IsAny<List<int>>()))
            .ReturnsAsync(new List<NoteOption>());

        // Act
        CreateOrderResultDTO result =
            await orderService.CreateOrderAsync(
                currentUserId,
                request);

        // Assert
        Assert.True(result.IsBranchFound);
        Assert.True(result.HasAccess);
        Assert.False(result.IsOrderValid);
        Assert.Null(result.Order);
        Assert.False(string.IsNullOrWhiteSpace(result.ErrorMessage));

        orderRepositoryMock.Verify(
            repository =>
                repository.GetActiveNoteOptionsAsync(
                    It.Is<List<int>>(ids =>
                        ids.Count == 1
                        && ids[0] == invalidNoteOptionId)),
            Times.Once);

        // Không chọn topping thì Service không truy vấn topping.
        orderRepositoryMock.Verify(
            repository =>
                repository.GetAvailableToppingsAsync(
                    It.IsAny<int>(),
                    It.IsAny<List<int>>()),
            Times.Never);

        // Request sai phải dừng trước khi cấp mã và lưu.
        orderRepositoryMock.Verify(
            repository =>
                repository.GetNextOrderCodeAsync(
                    It.IsAny<int>(),
                    It.IsAny<DateOnly>()),
            Times.Never);

        orderRepositoryMock.Verify(
            repository =>
                repository.AddAsync(It.IsAny<SalesOrder>()),
            Times.Never);

        unitOfWorkMock.VerifyNoOtherCalls();
    }

    // Một topping không được xuất hiện hai lần trong cùng một dòng món.
    [Fact]
    public async Task CreateOrderAsync_DuplicateTopping_ReturnsInvalidWithoutSaving()
    {
        // Arrange
        Mock<IBranchRepository> branchRepositoryMock =
            new Mock<IBranchRepository>();

        Mock<IOrderRepository> orderRepositoryMock =
            new Mock<IOrderRepository>();

        Mock<IUnitOfWork> unitOfWorkMock =
            new Mock<IUnitOfWork>();

        OrderService orderService = new OrderService(
            branchRepositoryMock.Object,
            orderRepositoryMock.Object,
            unitOfWorkMock.Object);

        int currentUserId = 10;
        int branchId = 1;
        int productSizeId = 10;
        int toppingId = 20;

        Branch branch = new Branch
        {
            Id = branchId,
            Code = "CN01",
            Name = "Chi nhánh trung tâm",
            Address = "Địa chỉ kiểm thử",
            IsActive = true,
            Deleted = false
        };

        CreateOrderRequestDTO request = new CreateOrderRequestDTO
        {
            BranchId = branchId,
            Items = new List<CreateOrderItemRequestDTO>
        {
            new CreateOrderItemRequestDTO
            {
                ProductSizeId = productSizeId,
                Quantity = 1,
                Toppings = new List<CreateOrderItemToppingRequestDTO>
                {
                    new CreateOrderItemToppingRequestDTO
                    {
                        ToppingId = toppingId,
                        Quantity = 1
                    },
                    new CreateOrderItemToppingRequestDTO
                    {
                        ToppingId = toppingId,
                        Quantity = 1
                    }
                }
            }
        }
        };

        branchRepositoryMock
            .Setup(repository =>
                repository.GetNotDeletedByIdAsync(branchId))
            .ReturnsAsync(branch);

        branchRepositoryMock
            .Setup(repository =>
                repository.HasActiveAssignmentAsync(
                    currentUserId,
                    branchId,
                    It.IsAny<DateOnly>()))
            .ReturnsAsync(true);

        orderRepositoryMock
            .Setup(repository =>
                repository.GetAvailableProductSizesAsync(
                    branchId,
                    It.IsAny<List<int>>()))
            .ReturnsAsync(new List<ProductSize>
            {
                new ProductSize
                {
                    Id = productSizeId
                }
            });

        // Request gửi lặp ID nhưng repository chỉ cần trả một entity duy nhất.
        orderRepositoryMock
            .Setup(repository =>
                repository.GetAvailableToppingsAsync(
                    branchId,
                    It.IsAny<List<int>>()))
            .ReturnsAsync(new List<Topping>
            {
                new Topping
                {
                    Id = toppingId,
                    ToppingGroupId = 1,
                    Name = "Trân châu",
                    Price = 5000m,
                    IsActive = true,
                    Deleted = false,
                    ToppingGroup = new ToppingGroup
                    {
                        Id = 1,
                        Name = "Topping thường",
                        IsActive = true,
                        Deleted = false
                    }
                }
            });

        // Act
        CreateOrderResultDTO result =
            await orderService.CreateOrderAsync(
                currentUserId,
                request);

        // Assert
        Assert.True(result.IsBranchFound);
        Assert.True(result.HasAccess);
        Assert.False(result.IsOrderValid);
        Assert.Null(result.Order);
        Assert.Equal(
            "Một topping không được chọn lặp lại trong cùng một món.",
            result.ErrorMessage);

        // Hai dòng topping giống nhau được gom thành một ID để truy vấn.
        orderRepositoryMock.Verify(
            repository =>
                repository.GetAvailableToppingsAsync(
                    branchId,
                    It.Is<List<int>>(ids =>
                        ids.Count == 1
                        && ids[0] == toppingId)),
            Times.Once);

        orderRepositoryMock.Verify(
            repository =>
                repository.GetNextOrderCodeAsync(
                    It.IsAny<int>(),
                    It.IsAny<DateOnly>()),
            Times.Never);

        orderRepositoryMock.Verify(
            repository =>
                repository.AddAsync(It.IsAny<SalesOrder>()),
            Times.Never);

        unitOfWorkMock.VerifyNoOtherCalls();
    }

    // Topping thường chỉ được chọn tối đa 3 phần trong một món.
    [Fact]
    public async Task CreateOrderAsync_NormalToppingQuantityAboveThree_ReturnsInvalidWithoutSaving()
    {
        // Arrange
        Mock<IBranchRepository> branchRepositoryMock =
            new Mock<IBranchRepository>();

        Mock<IOrderRepository> orderRepositoryMock =
            new Mock<IOrderRepository>();

        Mock<IUnitOfWork> unitOfWorkMock =
            new Mock<IUnitOfWork>();

        OrderService orderService = new OrderService(
            branchRepositoryMock.Object,
            orderRepositoryMock.Object,
            unitOfWorkMock.Object);

        int currentUserId = 10;
        int branchId = 1;
        int productSizeId = 10;
        int toppingId = 20;

        Branch branch = new Branch
        {
            Id = branchId,
            Code = "CN01",
            Name = "Chi nhánh trung tâm",
            Address = "Địa chỉ kiểm thử",
            IsActive = true,
            Deleted = false
        };

        CreateOrderRequestDTO request = new CreateOrderRequestDTO
        {
            BranchId = branchId,
            Items = new List<CreateOrderItemRequestDTO>
        {
            new CreateOrderItemRequestDTO
            {
                ProductSizeId = productSizeId,
                Quantity = 1,
                Toppings = new List<CreateOrderItemToppingRequestDTO>
                {
                    new CreateOrderItemToppingRequestDTO
                    {
                        ToppingId = toppingId,
                        Quantity = 4
                    }
                }
            }
        }
        };

        branchRepositoryMock
            .Setup(repository =>
                repository.GetNotDeletedByIdAsync(branchId))
            .ReturnsAsync(branch);

        branchRepositoryMock
            .Setup(repository =>
                repository.HasActiveAssignmentAsync(
                    currentUserId,
                    branchId,
                    It.IsAny<DateOnly>()))
            .ReturnsAsync(true);

        orderRepositoryMock
            .Setup(repository =>
                repository.GetAvailableProductSizesAsync(
                    branchId,
                    It.IsAny<List<int>>()))
            .ReturnsAsync(new List<ProductSize>
            {
                new ProductSize
                {
                    Id = productSizeId
                }
            });

        orderRepositoryMock
            .Setup(repository =>
                repository.GetAvailableToppingsAsync(
                    branchId,
                    It.IsAny<List<int>>()))
            .ReturnsAsync(new List<Topping>
            {
                new Topping
                {
                    Id = toppingId,
                    ToppingGroupId = 1,
                    Name = "Trân châu",
                    Price = 5000m,
                    IsActive = true,
                    Deleted = false,
                    ToppingGroup = new ToppingGroup
                    {
                        Id = 1,
                        Name = "Topping thường",
                        IsActive = true,
                        Deleted = false
                    }
                }
            });

        // Act
        CreateOrderResultDTO result =
            await orderService.CreateOrderAsync(
                currentUserId,
                request);

        // Assert
        Assert.True(result.IsBranchFound);
        Assert.True(result.HasAccess);
        Assert.False(result.IsOrderValid);
        Assert.Null(result.Order);
        Assert.Equal(
            "Số lượng topping Trân châu không hợp lệ.",
            result.ErrorMessage);

        orderRepositoryMock.Verify(
            repository =>
                repository.GetAvailableToppingsAsync(
                    branchId,
                    It.Is<List<int>>(ids =>
                        ids.Count == 1
                        && ids[0] == toppingId)),
            Times.Once);

        // Số lượng sai phải dừng trước mọi thao tác ghi.
        orderRepositoryMock.Verify(
            repository =>
                repository.GetNextOrderCodeAsync(
                    It.IsAny<int>(),
                    It.IsAny<DateOnly>()),
            Times.Never);

        orderRepositoryMock.Verify(
            repository =>
                repository.AddAsync(It.IsAny<SalesOrder>()),
            Times.Never);

        unitOfWorkMock.VerifyNoOtherCalls();
    }
    // Up-size chỉ được chọn tối đa một lần trong một món.
    [Fact]
    public async Task CreateOrderAsync_UpSizeQuantityAboveOne_ReturnsInvalidWithoutSaving()
    {
        // Arrange
        Mock<IBranchRepository> branchRepositoryMock =
            new Mock<IBranchRepository>();

        Mock<IOrderRepository> orderRepositoryMock =
            new Mock<IOrderRepository>();

        Mock<IUnitOfWork> unitOfWorkMock =
            new Mock<IUnitOfWork>();

        OrderService orderService = new OrderService(
            branchRepositoryMock.Object,
            orderRepositoryMock.Object,
            unitOfWorkMock.Object);

        int currentUserId = 10;
        int branchId = 1;
        int productSizeId = 10;
        int upSizeToppingId = 30;

        Branch branch = new Branch
        {
            Id = branchId,
            Code = "CN01",
            Name = "Chi nhánh trung tâm",
            Address = "Địa chỉ kiểm thử",
            IsActive = true,
            Deleted = false
        };

        CreateOrderRequestDTO request = new CreateOrderRequestDTO
        {
            BranchId = branchId,
            Items = new List<CreateOrderItemRequestDTO>
            {
                new CreateOrderItemRequestDTO
                {
                    ProductSizeId = productSizeId,
                    Quantity = 1,
                    Toppings = new List<CreateOrderItemToppingRequestDTO>
                    {
                        new CreateOrderItemToppingRequestDTO
                        {
                            ToppingId = upSizeToppingId,
                            Quantity = 2
                        }
                    }
                }
            }
        };

        branchRepositoryMock
            .Setup(repository =>
                repository.GetNotDeletedByIdAsync(branchId))
            .ReturnsAsync(branch);

        branchRepositoryMock
            .Setup(repository =>
                repository.HasActiveAssignmentAsync(
                    currentUserId,
                    branchId,
                    It.IsAny<DateOnly>()))
            .ReturnsAsync(true);

        orderRepositoryMock
            .Setup(repository =>
                repository.GetAvailableProductSizesAsync(
                    branchId,
                    It.IsAny<List<int>>()))
            .ReturnsAsync(new List<ProductSize>
            {
                new ProductSize
                {
                    Id = productSizeId
                }
            });

        orderRepositoryMock
            .Setup(repository =>
                repository.GetAvailableToppingsAsync(
                    branchId,
                    It.IsAny<List<int>>()))
            .ReturnsAsync(new List<Topping>
            {
                new Topping
                {
                    Id = upSizeToppingId,
                    ToppingGroupId = 2,
                    Name = "Up size 1300ml",
                    Price = 10000m,
                    IsActive = true,
                    Deleted = false,
                    ToppingGroup = new ToppingGroup
                    {
                        Id = 2,
                        Name = "Up size",
                        IsActive = true,
                        Deleted = false
                    }
                }
            });

        // Act
        CreateOrderResultDTO result =
            await orderService.CreateOrderAsync(
                currentUserId,
                request);

        // Assert
        Assert.True(result.IsBranchFound);
        Assert.True(result.HasAccess);
        Assert.False(result.IsOrderValid);
        Assert.Null(result.Order);
        Assert.Equal(
            "Số lượng topping Up size 1300ml không hợp lệ.",
            result.ErrorMessage);

        orderRepositoryMock.Verify(
            repository =>
                repository.GetAvailableToppingsAsync(
                    branchId,
                    It.Is<List<int>>(ids =>
                        ids.Count == 1
                        && ids[0] == upSizeToppingId)),
            Times.Once);

        // Quantity sai phải dừng trước mọi thao tác ghi.
        orderRepositoryMock.Verify(
            repository =>
                repository.GetNextOrderCodeAsync(
                    It.IsAny<int>(),
                    It.IsAny<DateOnly>()),
            Times.Never);

        orderRepositoryMock.Verify(
            repository =>
                repository.AddAsync(It.IsAny<SalesOrder>()),
            Times.Never);

        unitOfWorkMock.VerifyNoOtherCalls();
    }

    // Mỗi nhóm ghi chú chỉ được chọn một option trong một dòng món.
    [Fact]
    public async Task CreateOrderAsync_TwoNoteOptionsInSameGroup_ReturnsInvalidWithoutSaving()
    {
        // Arrange
        Mock<IBranchRepository> branchRepositoryMock =
            new Mock<IBranchRepository>();

        Mock<IOrderRepository> orderRepositoryMock =
            new Mock<IOrderRepository>();

        Mock<IUnitOfWork> unitOfWorkMock =
            new Mock<IUnitOfWork>();

        OrderService orderService = new OrderService(
            branchRepositoryMock.Object,
            orderRepositoryMock.Object,
            unitOfWorkMock.Object);

        int currentUserId = 10;
        int branchId = 1;
        int productSizeId = 10;
        int firstNoteOptionId = 101;
        int secondNoteOptionId = 102;
        int noteGroupId = 1;

        Branch branch = new Branch
        {
            Id = branchId,
            Code = "CN01",
            Name = "Chi nhánh trung tâm",
            Address = "Địa chỉ kiểm thử",
            IsActive = true,
            Deleted = false
        };

        CreateOrderRequestDTO request = new CreateOrderRequestDTO
        {
            BranchId = branchId,
            Items = new List<CreateOrderItemRequestDTO>
            {
                new CreateOrderItemRequestDTO
                {
                    ProductSizeId = productSizeId,
                    Quantity = 1,
                    NoteOptionIds = new List<int>
                    {
                        firstNoteOptionId,
                        secondNoteOptionId
                    }
                }
            }
        };

        branchRepositoryMock
            .Setup(repository =>
                repository.GetNotDeletedByIdAsync(branchId))
            .ReturnsAsync(branch);

        branchRepositoryMock
            .Setup(repository =>
                repository.HasActiveAssignmentAsync(
                    currentUserId,
                    branchId,
                    It.IsAny<DateOnly>()))
            .ReturnsAsync(true);

        orderRepositoryMock
            .Setup(repository =>
                repository.GetAvailableProductSizesAsync(
                    branchId,
                    It.IsAny<List<int>>()))
            .ReturnsAsync(new List<ProductSize>
            {
                new ProductSize
                {
                    Id = productSizeId
                }
            });

        // Hai option đều tồn tại nhưng cùng thuộc một NoteGroup.
        orderRepositoryMock
            .Setup(repository =>
                repository.GetActiveNoteOptionsAsync(
                    It.IsAny<List<int>>()))
            .ReturnsAsync(new List<NoteOption>
            {
                new NoteOption
                {
                    Id = firstNoteOptionId,
                    NoteGroupId = noteGroupId,
                    Name = "Ít đá",
                    IsActive = true,
                    Deleted = false
                },
                new NoteOption
                {
                    Id = secondNoteOptionId,
                    NoteGroupId = noteGroupId,
                    Name = "Không đá",
                    IsActive = true,
                    Deleted = false
                }
            });

        // Act
        CreateOrderResultDTO result =
            await orderService.CreateOrderAsync(
                currentUserId,
                request);

        // Assert
        Assert.True(result.IsBranchFound);
        Assert.True(result.HasAccess);
        Assert.False(result.IsOrderValid);
        Assert.Null(result.Order);
        Assert.Equal(
            "Mỗi nhóm ghi chú chỉ được chọn một lựa chọn.",
            result.ErrorMessage);

        orderRepositoryMock.Verify(
            repository =>
                repository.GetActiveNoteOptionsAsync(
                    It.Is<List<int>>(ids =>
                        ids.Count == 2
                        && ids.Contains(firstNoteOptionId)
                        && ids.Contains(secondNoteOptionId))),
            Times.Once);

        orderRepositoryMock.Verify(
            repository =>
                repository.GetNextOrderCodeAsync(
                    It.IsAny<int>(),
                    It.IsAny<DateOnly>()),
            Times.Never);

        orderRepositoryMock.Verify(
            repository =>
                repository.AddAsync(It.IsAny<SalesOrder>()),
            Times.Never);

        unitOfWorkMock.VerifyNoOtherCalls();
    }

    // Request hợp lệ phải tạo graph đơn, tính lại giá và commit transaction.
    [Fact]
    public async Task CreateOrderAsync_ValidSimpleOrder_CreatesOrderAndCommits()
    {
        // Arrange
        Mock<IBranchRepository> branchRepositoryMock =
            new Mock<IBranchRepository>();

        Mock<IOrderRepository> orderRepositoryMock =
            new Mock<IOrderRepository>();

        Mock<IUnitOfWork> unitOfWorkMock =
            new Mock<IUnitOfWork>();

        OrderService orderService = new OrderService(
            branchRepositoryMock.Object,
            orderRepositoryMock.Object,
            unitOfWorkMock.Object);

        int currentUserId = 10;
        int branchId = 1;
        int productSizeId = 10;

        Branch branch = new Branch
        {
            Id = branchId,
            Code = "CN01",
            Name = "Chi nhánh trung tâm",
            Address = "Địa chỉ kiểm thử",
            IsActive = true,
            Deleted = false
        };

        ProductSize productSize = new ProductSize
        {
            Id = productSizeId,
            ProductId = 100,
            SizeId = 1,
            Price = 35000m,
            IsActive = true,
            Deleted = false,
            Product = new Product
            {
                Id = 100,
                Name = "Trà mãng cầu",
                IsActive = true,
                Deleted = false
            },
            Size = new Size
            {
                Id = 1,
                Name = "M",
                IsActive = true,
                Deleted = false
            }
        };

        CreateOrderRequestDTO request = new CreateOrderRequestDTO
        {
            BranchId = branchId,
            Items = new List<CreateOrderItemRequestDTO>
            {
                new CreateOrderItemRequestDTO
                {
                    ProductSizeId = productSizeId,
                    Quantity = 2
                }
            }
        };

        branchRepositoryMock
            .Setup(repository =>
                repository.GetNotDeletedByIdAsync(branchId))
            .ReturnsAsync(branch);

        branchRepositoryMock
            .Setup(repository =>
                repository.HasActiveAssignmentAsync(
                    currentUserId,
                    branchId,
                    It.IsAny<DateOnly>()))
            .ReturnsAsync(true);

        orderRepositoryMock
            .Setup(repository =>
                repository.GetAvailableProductSizesAsync(
                    branchId,
                    It.IsAny<List<int>>()))
            .ReturnsAsync(new List<ProductSize>
            {
            productSize
            });

        unitOfWorkMock
            .Setup(unitOfWork =>
                unitOfWork.BeginTransactionAsync())
            .Returns(Task.CompletedTask);

        orderRepositoryMock
            .Setup(repository =>
                repository.GetNextOrderCodeAsync(
                    branchId,
                    It.IsAny<DateOnly>()))
            .ReturnsAsync(new OrderCodeResult
            {
                Code = "CN01-20260920-0001",
                SequenceNumber = 1
            });

        SalesOrder? capturedOrder = null;

        // Giữ lại graph được Service gửi xuống repository để kiểm tra.
        orderRepositoryMock
            .Setup(repository =>
                repository.AddAsync(It.IsAny<SalesOrder>()))
            .Callback<SalesOrder>(order =>
                capturedOrder = order)
            .Returns(Task.CompletedTask);

        unitOfWorkMock
            .Setup(unitOfWork =>
                unitOfWork.SaveChangesAsync())
            .ReturnsAsync(1);

        unitOfWorkMock
            .Setup(unitOfWork =>
                unitOfWork.CommitTransactionAsync())
            .Returns(Task.CompletedTask);

        // Act
        CreateOrderResultDTO result =
            await orderService.CreateOrderAsync(
                currentUserId,
                request);

        // Assert kết quả nghiệp vụ.
        Assert.True(result.IsBranchFound);
        Assert.True(result.HasAccess);
        Assert.True(result.IsOrderValid);
        Assert.Null(result.ErrorMessage);
        Assert.NotNull(result.Order);

        Assert.Equal("CN01-20260920-0001", result.Order.Code);
        Assert.Equal(branchId, result.Order.BranchId);
        Assert.Equal(70000m, result.Order.TotalAmount);
        Assert.Equal("COMPLETED", result.Order.Status);

        // Assert graph entity được tạo từ dữ liệu database.
        Assert.NotNull(capturedOrder);
        Assert.Equal(currentUserId, capturedOrder.CreatedByUserId);
        Assert.Equal(branchId, capturedOrder.BranchId);
        Assert.Equal(1, capturedOrder.DailySequence);
        Assert.Equal(70000m, capturedOrder.TotalAmount);
        Assert.Equal("COMPLETED", capturedOrder.Status);
        Assert.Single(capturedOrder.OrderItems);

        OrderItem capturedItem =
            capturedOrder.OrderItems.First();

        Assert.Equal(productSizeId, capturedItem.ProductSizeId);
        Assert.Equal("Trà mãng cầu", capturedItem.ProductNameSnapshot);
        Assert.Equal("M", capturedItem.SizeNameSnapshot);
        Assert.Equal(2, capturedItem.Quantity);
        Assert.Equal(35000m, capturedItem.UnitPriceSnapshot);
        Assert.Equal(70000m, capturedItem.SubtotalAmount);
        Assert.Empty(capturedItem.OrderItemToppings);
        Assert.Empty(capturedItem.OrderItemNotes);

        // Assert luồng transaction thành công.
        unitOfWorkMock.Verify(
            unitOfWork =>
                unitOfWork.BeginTransactionAsync(),
            Times.Once);

        orderRepositoryMock.Verify(
            repository =>
                repository.GetNextOrderCodeAsync(
                    branchId,
                    It.IsAny<DateOnly>()),
            Times.Once);

        orderRepositoryMock.Verify(
            repository =>
                repository.AddAsync(It.IsAny<SalesOrder>()),
            Times.Once);

        unitOfWorkMock.Verify(
            unitOfWork =>
                unitOfWork.SaveChangesAsync(),
            Times.Once);

        unitOfWorkMock.Verify(
            unitOfWork =>
                unitOfWork.CommitTransactionAsync(),
            Times.Once);

        unitOfWorkMock.Verify(
            unitOfWork =>
                unitOfWork.RollbackTransactionAsync(),
            Times.Never);
    }

    // Request hợp lệ phải tạo graph đơn, tính lại giá và commit transaction.
    [Fact]
    public async Task CreateOrderAsync_ValidConfiguredOrder_CalculatesPriceAndCreatesSnapshots()
    {
        // Arrange
        Mock<IBranchRepository> branchRepositoryMock =
            new Mock<IBranchRepository>();

        Mock<IOrderRepository> orderRepositoryMock =
            new Mock<IOrderRepository>();

        Mock<IUnitOfWork> unitOfWorkMock =
            new Mock<IUnitOfWork>();

        OrderService orderService = new OrderService(
            branchRepositoryMock.Object,
            orderRepositoryMock.Object,
            unitOfWorkMock.Object);

        int currentUserId = 10;
        int branchId = 1;
        int productSizeId = 10;
        int toppingId = 20;
        int noteOptionId = 101;

        Branch branch = new Branch
        {
            Id = branchId,
            Code = "CN01",
            Name = "Chi nhánh trung tâm",
            Address = "Địa chỉ kiểm thử",
            IsActive = true,
            Deleted = false
        };

        ProductSize productSize = new ProductSize
        {
            Id = productSizeId,
            ProductId = 100,
            SizeId = 1,
            Price = 35000m,
            IsActive = true,
            Deleted = false,
            Product = new Product
            {
                Id = 100,
                Name = "Trà mãng cầu",
                IsActive = true,
                Deleted = false
            },
            Size = new Size
            {
                Id = 1,
                Name = "M",
                IsActive = true,
                Deleted = false
            }
        };

        CreateOrderRequestDTO request = new CreateOrderRequestDTO
        {
            BranchId = branchId,
            Items = new List<CreateOrderItemRequestDTO>
            {
                new CreateOrderItemRequestDTO
                {
                    ProductSizeId = productSizeId,
                    Quantity = 2,
                    Toppings = new List<CreateOrderItemToppingRequestDTO>
                    {
                        new CreateOrderItemToppingRequestDTO
                        {
                            ToppingId = toppingId,
                            Quantity = 2
                        }
                    },
                    NoteOptionIds = new List<int>
                    {
                        noteOptionId
                    }
                }
            }
        };

        branchRepositoryMock
            .Setup(repository =>
                repository.GetNotDeletedByIdAsync(branchId))
            .ReturnsAsync(branch);

        branchRepositoryMock
            .Setup(repository =>
                repository.HasActiveAssignmentAsync(
                    currentUserId,
                    branchId,
                    It.IsAny<DateOnly>()))
            .ReturnsAsync(true);

        orderRepositoryMock
            .Setup(repository =>
                repository.GetAvailableProductSizesAsync(
                    branchId,
                    It.IsAny<List<int>>()))
            .ReturnsAsync(new List<ProductSize>
            {
            productSize
            });

        orderRepositoryMock
            .Setup(repository =>
                repository.GetAvailableToppingsAsync(
                    branchId,
                    It.IsAny<List<int>>()))
            .ReturnsAsync(new List<Topping>
            {
                new Topping
                {
                    Id = toppingId,
                    ToppingGroupId = 1,
                    Name = "Trân châu",
                    Price = 5000m,
                    IsActive = true,
                    Deleted = false,
                    ToppingGroup = new ToppingGroup
                    {
                        Id = 1,
                        Name = "Topping thường",
                        IsActive = true,
                        Deleted = false
                    }
                }
            });

        orderRepositoryMock
            .Setup(repository =>
                repository.GetActiveNoteOptionsAsync(
                    It.IsAny<List<int>>()))
            .ReturnsAsync(new List<NoteOption>
            {
                new NoteOption
                {
                    Id = noteOptionId,
                    NoteGroupId = 1,
                    Name = "Ít đá",
                    IsActive = true,
                    Deleted = false
                }
            });

        unitOfWorkMock
            .Setup(unitOfWork =>
                unitOfWork.BeginTransactionAsync())
            .Returns(Task.CompletedTask);

        orderRepositoryMock
            .Setup(repository =>
                repository.GetNextOrderCodeAsync(
                    branchId,
                    It.IsAny<DateOnly>()))
            .ReturnsAsync(new OrderCodeResult
            {
                Code = "CN01-20260920-0001",
                SequenceNumber = 1
            });

        SalesOrder? capturedOrder = null;

        // Giữ lại graph được Service gửi xuống repository để kiểm tra.
        orderRepositoryMock
            .Setup(repository =>
                repository.AddAsync(It.IsAny<SalesOrder>()))
            .Callback<SalesOrder>(order =>
                capturedOrder = order)
            .Returns(Task.CompletedTask);

        unitOfWorkMock
            .Setup(unitOfWork =>
                unitOfWork.SaveChangesAsync())
            .ReturnsAsync(1);

        unitOfWorkMock
            .Setup(unitOfWork =>
                unitOfWork.CommitTransactionAsync())
            .Returns(Task.CompletedTask);

        // Act
        CreateOrderResultDTO result =
            await orderService.CreateOrderAsync(
                currentUserId,
                request);

        // Assert kết quả nghiệp vụ.
        Assert.True(result.IsBranchFound);
        Assert.True(result.HasAccess);
        Assert.True(result.IsOrderValid);
        Assert.Null(result.ErrorMessage);
        Assert.NotNull(result.Order);

        Assert.Equal("CN01-20260920-0001", result.Order.Code);
        Assert.Equal(branchId, result.Order.BranchId);
        Assert.Equal(90000m, result.Order.TotalAmount);
        Assert.Equal("COMPLETED", result.Order.Status);

        // Assert graph entity được tạo từ dữ liệu database.
        Assert.NotNull(capturedOrder);
        Assert.Equal(currentUserId, capturedOrder.CreatedByUserId);
        Assert.Equal(branchId, capturedOrder.BranchId);
        Assert.Equal(1, capturedOrder.DailySequence);
        Assert.Equal(90000m, capturedOrder.TotalAmount);
        Assert.Equal("COMPLETED", capturedOrder.Status);
        Assert.Single(capturedOrder.OrderItems);

        OrderItem capturedItem =
            capturedOrder.OrderItems.First();

        Assert.Equal(productSizeId, capturedItem.ProductSizeId);
        Assert.Equal("Trà mãng cầu", capturedItem.ProductNameSnapshot);
        Assert.Equal("M", capturedItem.SizeNameSnapshot);
        Assert.Equal(35000m, capturedItem.UnitPriceSnapshot);
        Assert.Equal(2, capturedItem.Quantity);
        Assert.Equal(90000m, capturedItem.SubtotalAmount);

        Assert.Single(capturedItem.OrderItemToppings);

        OrderItemTopping capturedTopping =
            capturedItem.OrderItemToppings.First();

        Assert.Equal(toppingId, capturedTopping.ToppingId);
        Assert.Equal("Trân châu", capturedTopping.ToppingNameSnapshot);
        Assert.Equal(2, capturedTopping.Quantity);
        Assert.Equal(5000m, capturedTopping.UnitPriceSnapshot);

        Assert.Single(capturedItem.OrderItemNotes);

        OrderItemNote capturedNote =
            capturedItem.OrderItemNotes.First();

        Assert.Equal(noteOptionId, capturedNote.NoteOptionId);
        Assert.Equal("Ít đá", capturedNote.NoteNameSnapshot);

        // Assert luồng transaction thành công.
        unitOfWorkMock.Verify(
            unitOfWork =>
                unitOfWork.BeginTransactionAsync(),
            Times.Once);

        orderRepositoryMock.Verify(
            repository =>
                repository.GetNextOrderCodeAsync(
                    branchId,
                    It.IsAny<DateOnly>()),
            Times.Once);

        orderRepositoryMock.Verify(
            repository =>
                repository.AddAsync(It.IsAny<SalesOrder>()),
            Times.Once);

        unitOfWorkMock.Verify(
            unitOfWork =>
                unitOfWork.SaveChangesAsync(),
            Times.Once);

        unitOfWorkMock.Verify(
            unitOfWork =>
                unitOfWork.CommitTransactionAsync(),
            Times.Once);

        unitOfWorkMock.Verify(
            unitOfWork =>
                unitOfWork.RollbackTransactionAsync(),
            Times.Never);
    }

    // Nếu lưu đơn thất bại thì phải rollback và không được commit.
    [Fact]
    public async Task CreateOrderAsync_SaveFails_RollsBackAndRethrows()
    {
        // Arrange
        Mock<IBranchRepository> branchRepositoryMock =
            new Mock<IBranchRepository>();

        Mock<IOrderRepository> orderRepositoryMock =
            new Mock<IOrderRepository>();

        Mock<IUnitOfWork> unitOfWorkMock =
            new Mock<IUnitOfWork>();

        OrderService orderService = new OrderService(
            branchRepositoryMock.Object,
            orderRepositoryMock.Object,
            unitOfWorkMock.Object);

        int currentUserId = 10;
        int branchId = 1;
        int productSizeId = 10;

        Branch branch = new Branch
        {
            Id = branchId,
            Code = "CN01",
            Name = "Chi nhánh trung tâm",
            Address = "Địa chỉ kiểm thử",
            IsActive = true,
            Deleted = false
        };

        ProductSize productSize = new ProductSize
        {
            Id = productSizeId,
            ProductId = 100,
            SizeId = 1,
            Price = 35000m,
            IsActive = true,
            Deleted = false,
            Product = new Product
            {
                Id = 100,
                Name = "Trà mãng cầu",
                IsActive = true,
                Deleted = false
            },
            Size = new Size
            {
                Id = 1,
                Name = "M",
                IsActive = true,
                Deleted = false
            }
        };

        CreateOrderRequestDTO request = new CreateOrderRequestDTO
        {
            BranchId = branchId,
            Items = new List<CreateOrderItemRequestDTO>
        {
            new CreateOrderItemRequestDTO
            {
                ProductSizeId = productSizeId,
                Quantity = 1
            }
        }
        };

        branchRepositoryMock
            .Setup(repository =>
                repository.GetNotDeletedByIdAsync(branchId))
            .ReturnsAsync(branch);

        branchRepositoryMock
            .Setup(repository =>
                repository.HasActiveAssignmentAsync(
                    currentUserId,
                    branchId,
                    It.IsAny<DateOnly>()))
            .ReturnsAsync(true);

        orderRepositoryMock
            .Setup(repository =>
                repository.GetAvailableProductSizesAsync(
                    branchId,
                    It.IsAny<List<int>>()))
            .ReturnsAsync(new List<ProductSize>
            {
            productSize
            });

        unitOfWorkMock
            .Setup(unitOfWork =>
                unitOfWork.BeginTransactionAsync())
            .Returns(Task.CompletedTask);

        orderRepositoryMock
            .Setup(repository =>
                repository.GetNextOrderCodeAsync(
                    branchId,
                    It.IsAny<DateOnly>()))
            .ReturnsAsync(new OrderCodeResult
            {
                Code = "CN01-20260920-0001",
                SequenceNumber = 1
            });

        orderRepositoryMock
            .Setup(repository =>
                repository.AddAsync(It.IsAny<SalesOrder>()))
            .Returns(Task.CompletedTask);

        // Mô phỏng lỗi database khi lưu graph đơn.
        unitOfWorkMock
            .Setup(unitOfWork =>
                unitOfWork.SaveChangesAsync())
            .ThrowsAsync(new InvalidOperationException(
                "Lỗi lưu dữ liệu kiểm thử."));

        unitOfWorkMock
            .Setup(unitOfWork =>
                unitOfWork.RollbackTransactionAsync())
            .Returns(Task.CompletedTask);

        // Act
        InvalidOperationException exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                async () =>
                    await orderService.CreateOrderAsync(
                        currentUserId,
                        request));

        // Assert
        Assert.Equal(
            "Lỗi lưu dữ liệu kiểm thử.",
            exception.Message);

        unitOfWorkMock.Verify(
            unitOfWork =>
                unitOfWork.BeginTransactionAsync(),
            Times.Once);

        orderRepositoryMock.Verify(
            repository =>
                repository.AddAsync(It.IsAny<SalesOrder>()),
            Times.Once);

        unitOfWorkMock.Verify(
            unitOfWork =>
                unitOfWork.SaveChangesAsync(),
            Times.Once);

        unitOfWorkMock.Verify(
            unitOfWork =>
                unitOfWork.RollbackTransactionAsync(),
            Times.Once);

        unitOfWorkMock.Verify(
            unitOfWork =>
                unitOfWork.CommitTransactionAsync(),
            Times.Never);
    }

    // Test đơn không COMPLETED
    [Fact]
    public async Task ReportOrderAsync_OrderNotCompleted_ReturnsConflictWithoutSaving()
    {
        // Arrange
        Mock<IBranchRepository> branchRepositoryMock =
            new Mock<IBranchRepository>();

        Mock<IOrderRepository> orderRepositoryMock =
            new Mock<IOrderRepository>();

        Mock<IUnitOfWork> unitOfWorkMock =
            new Mock<IUnitOfWork>();

        OrderService orderService = new OrderService(
            branchRepositoryMock.Object,
            orderRepositoryMock.Object,
            unitOfWorkMock.Object);

        int currentUserId = 1002;
        int orderId = 25;

        DateTime originalReportedAt =
            new DateTime(2026, 9, 22, 8, 0, 0, DateTimeKind.Utc);

        SalesOrder order = new SalesOrder
        {
            Id = orderId,
            Code = "CN02-20260922-0001",
            CreatedByUserId = currentUserId,
            Status = "NEEDS_REVIEW",
            ReportReason = "Lý do ban đầu",
            ReportedByUserId = currentUserId,
            ReportedAt = originalReportedAt
        };

        orderRepositoryMock
            .Setup(repository =>
                repository.GetOrderForReportAsync(
                    orderId,
                    currentUserId))
            .ReturnsAsync(order);

        ReportOrderRequestDTO request = new ReportOrderRequestDTO
        {
            Reason = "Lý do mới"
        };

        // Act
        ReportOrderResult result =
            await orderService.ReportOrderAsync(
                currentUserId,
                orderId,
                request);

        // Assert
        Assert.True(result.IsReportValid);
        Assert.True(result.IsOrderFound);
        Assert.False(result.CanReport);
        Assert.Null(result.Order);

        // Dữ liệu báo lần đầu không bị ghi đè
        Assert.Equal("Lý do ban đầu", order.ReportReason);
        Assert.Equal(originalReportedAt, order.ReportedAt);

        unitOfWorkMock.Verify(
            unitOfWork => unitOfWork.SaveChangesAsync(),
            Times.Never);
    }

    // Test báo sai đơn thành công
    [Fact]
    public async Task ReportOrderAsync_CompletedOrder_UpdatesReportAndSaves()
    {
        // Arrange
        Mock<IBranchRepository> branchRepositoryMock =
            new Mock<IBranchRepository>();

        Mock<IOrderRepository> orderRepositoryMock =
            new Mock<IOrderRepository>();

        Mock<IUnitOfWork> unitOfWorkMock =
            new Mock<IUnitOfWork>();

        OrderService orderService = new OrderService(
            branchRepositoryMock.Object,
            orderRepositoryMock.Object,
            unitOfWorkMock.Object);

        int currentUserId = 1002;
        int orderId = 25;

        SalesOrder order = new SalesOrder
        {
            Id = orderId,
            Code = "CN02-20260922-0001",
            CreatedByUserId = currentUserId,
            Status = "COMPLETED"
        };

        orderRepositoryMock
            .Setup(repository =>
                repository.GetOrderForReportAsync(
                    orderId,
                    currentUserId))
            .ReturnsAsync(order);

        unitOfWorkMock
            .Setup(unitOfWork =>
                unitOfWork.SaveChangesAsync())
            .ReturnsAsync(1);

        ReportOrderRequestDTO request = new ReportOrderRequestDTO
        {
            Reason = "  Sai size của món  "
        };

        // Act
        ReportOrderResult result =
            await orderService.ReportOrderAsync(
                currentUserId,
                orderId,
                request);

        // Assert
        Assert.True(result.IsReportValid);
        Assert.True(result.IsOrderFound);
        Assert.True(result.CanReport);
        Assert.NotNull(result.Order);

        Assert.Equal("NEEDS_REVIEW", order.Status);
        Assert.Equal("Sai size của món", order.ReportReason);
        Assert.Equal(currentUserId, order.ReportedByUserId);
        Assert.NotNull(order.ReportedAt);
        Assert.Equal(order.ReportedAt, order.UpdatedAt);

        Assert.Equal(order.Id, result.Order.Id);
        Assert.Equal(order.Code, result.Order.Code);
        Assert.Equal("NEEDS_REVIEW", result.Order.Status);
        Assert.Equal("Sai size của món", result.Order.ReportReason);

        unitOfWorkMock.Verify(
            unitOfWork => unitOfWork.SaveChangesAsync(),
            Times.Once);

        unitOfWorkMock.Verify(
            unitOfWork => unitOfWork.BeginTransactionAsync(),
            Times.Never);

        unitOfWorkMock.Verify(
            unitOfWork => unitOfWork.CommitTransactionAsync(),
            Times.Never);
    }
}