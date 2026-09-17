using Moq;
using TuanKietBranchFlow.Application.DTOs.OrderMenus;
using TuanKietBranchFlow.Application.Services;
using TuanKietBranchFlow.Infrastructure.Models;
using TuanKietBranchFlow.Infrastructure.Repositories;

namespace TuanKietBranchFlow.Tests;

public class OrderMenuServiceTests
{
    // Chi nhánh không tồn tại thì dừng trước khi kiểm tra phân công và đọc menu
    [Fact]
    public async Task GetOrderMenuAsync_BranchNotFound_ReturnsNotFound()
    {
        // Arrange
        Mock<IBranchRepository> branchRepositoryMock =
            new Mock<IBranchRepository>();

        Mock<IOrderMenuRepository> orderMenuRepositoryMock =
            new Mock<IOrderMenuRepository>();

        OrderMenuService service = new OrderMenuService(
            branchRepositoryMock.Object,
            orderMenuRepositoryMock.Object);

        int currentUserId = 10;
        int branchId = 99;

        branchRepositoryMock
            .Setup(repository =>
                repository.GetNotDeletedByIdAsync(branchId))
            .ReturnsAsync((Branch?)null);

        // Act
        OrderMenuResultDTO result =
            await service.GetOrderMenuAsync(currentUserId, branchId);

        // Assert
        Assert.False(result.IsBranchFound);
        Assert.False(result.HasAccess);
        Assert.Null(result.Menu);

        branchRepositoryMock.Verify(
            repository =>
                repository.HasActiveAssignmentAsync(
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<DateOnly>()), Times.Never);

        orderMenuRepositoryMock.VerifyNoOtherCalls();
    }

    // Không có phân công thì dừng trước khi truy vấn dữ liệu menu
    [Fact]
    public async Task GetOrderMenuAsync_WithoutAssignment_ReturnsForbidden()
    {
        // Arrange
        Mock<IBranchRepository> branchRepositoryMock =
            new Mock<IBranchRepository>();

        Mock<IOrderMenuRepository> orderMenuRepositoryMock =
            new Mock<IOrderMenuRepository>();

        OrderMenuService service = new OrderMenuService(
            branchRepositoryMock.Object,
            orderMenuRepositoryMock.Object);

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

        //Act
        OrderMenuResultDTO result =
            await service.GetOrderMenuAsync(currentUserId, branchId);

        // Assert
        Assert.True(result.IsBranchFound);
        Assert.False(result.HasAccess);
        Assert.Null(result.Menu);

        orderMenuRepositoryMock.Verify(
            repository =>
                repository.GetAvailableCategoriesAsync(
                    It.IsAny<int>()), Times.Never);

        orderMenuRepositoryMock.Verify(
            repository =>
                repository.GetAvailableToppingGroupsAsync(
                    It.IsAny<int>()), Times.Never);

        orderMenuRepositoryMock.Verify(
            repository =>
                repository.GetActiveNoteGroupsAsync(), Times.Never);
    }

    // Employee có quyền thì trả menu và ánh xạ đúng giá, topping, ghi chú
    [Fact]
    public async Task GetOrderMenuAsync_WithAssignment_ReturnsMappedMenu()
    {
        // Arrange
        Mock<IBranchRepository> branchRepositoryMock =
            new Mock<IBranchRepository>();

        Mock<IOrderMenuRepository> orderMenuRepositoryMock =
            new Mock<IOrderMenuRepository>();

        OrderMenuService service = new OrderMenuService(
            branchRepositoryMock.Object,
            orderMenuRepositoryMock.Object);

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

        Size smallSize = new Size
        {
            Id = 1,
            Name = "500ML",
            IsActive = true,
            Deleted = false
        };

        Size largeSize = new Size
        {
            Id = 2,
            Name = "700ML",
            IsActive = true,
            Deleted = false
        };

        Product product = new Product
        {
            Id = 1,
            CategoryId = 1,
            Name = "Trà mãng cầu",
            ImageUrl = "/images/tra-mang-cau.jpg",
            IsActive = true,
            Deleted = false
        };

        product.ProductSizes.Add(new ProductSize
        {
            Id = 11,
            ProductId = product.Id,
            SizeId = smallSize.Id,
            Price = 35000m,
            IsActive = true,
            Deleted = false,
            Size = smallSize
        });

        product.ProductSizes.Add(new ProductSize
        {
            Id = 12,
            ProductId = product.Id,
            SizeId = largeSize.Id,
            Price = 45000m,
            IsActive = true,
            Deleted = false,
            Size = largeSize
        });

        Category category = new Category
        {
            Id = 1,
            Name = "Trà trái cây",
            IsActive = true,
            Deleted = false
        };

        category.Products.Add(product);

        ToppingGroup normalToppingGroup = new ToppingGroup
        {
            Id = 1,
            Name = "Topping",
            IsActive = true,
            Deleted = false
        };

        normalToppingGroup.Toppings.Add(new Topping
        {
            Id = 1,
            ToppingGroupId = normalToppingGroup.Id,
            Name = "Trân châu đen",
            Price = 5000m,
            IsActive = true,
            Deleted = false
        });

        ToppingGroup upSizeGroup = new ToppingGroup
        {
            Id = 2,
            Name = "Up size",
            IsActive = true,
            Deleted = false
        };

        upSizeGroup.Toppings.Add(new Topping
        {
            Id = 2,
            ToppingGroupId = upSizeGroup.Id,
            Name = "UP SIZE 1300ML",
            Price = 10000m,
            IsActive = true,
            Deleted = false
        });

        NoteGroup noteGroup = new NoteGroup
        {
            Id = 1,
            Name = "Đá",
            IsActive = true,
            Deleted = false
        };

        noteGroup.NoteOptions.Add(new NoteOption
        {
            Id = 1,
            NoteGroupId = noteGroup.Id,
            Name = "Ít đá",
            IsActive = true,
            Deleted = false
        });

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

        orderMenuRepositoryMock
            .Setup(repository =>
                repository.GetAvailableCategoriesAsync(branchId))
            .ReturnsAsync(new List<Category> { category });

        orderMenuRepositoryMock
            .Setup(repository =>
                repository.GetAvailableToppingGroupsAsync(branchId))
            .ReturnsAsync(new List<ToppingGroup>
            {
                normalToppingGroup,
                upSizeGroup
            });

        orderMenuRepositoryMock
            .Setup(repository =>
                repository.GetActiveNoteGroupsAsync())
            .ReturnsAsync(new List<NoteGroup> { noteGroup });

        // Act
        OrderMenuResultDTO result =
            await service.GetOrderMenuAsync(currentUserId, branchId);

        // Assert trạng thái và chi nhánh
        Assert.True(result.IsBranchFound);
        Assert.True(result.HasAccess);

        OrderMenuResponseDTO menu =
        Assert.IsType<OrderMenuResponseDTO>(result.Menu);

        Assert.Equal(branchId, menu.Branch.Id);
        Assert.Equal("CN01", menu.Branch.Code);

        // Assert món và giá nhỏ nhất
        OrderMenuCategoryDTO categoryDTO =
            Assert.Single(menu.Categories);

        OrderMenuProductDTO productDTO =
            Assert.Single(categoryDTO.Products);

        Assert.Equal("Trà mãng cầu", productDTO.Name);
        Assert.Equal(35000m, productDTO.MinPrice);
        Assert.Equal(2, productDTO.ProductSizes.Count);

        // Assert giới hạn topping thường và up-size
        Assert.Equal(2, menu.ToppingGroups.Count);

        OrderMenuToppingDTO normalToppingDTO =
            Assert.Single(menu.ToppingGroups[0].Toppings);

        OrderMenuToppingDTO upSizeDTO =
            Assert.Single(menu.ToppingGroups[1].Toppings);

        Assert.Equal(3, normalToppingDTO.MaxQuantity);
        Assert.Equal(1, upSizeDTO.MaxQuantity);

        // Assert ghi chú
        OrderMenuNoteGroupDTO noteGroupDTO =
            Assert.Single(menu.NoteGroups);

        Assert.Equal(1, noteGroupDTO.MaxSelections);
        Assert.Single(noteGroupDTO.Options);
    }
}