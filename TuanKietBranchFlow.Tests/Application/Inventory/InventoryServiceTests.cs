using Microsoft.EntityFrameworkCore;
using Moq;
using TuanKietBranchFlow.Application.DTOs.Inventory;
using TuanKietBranchFlow.Application.Services.Inventory;
using TuanKietBranchFlow.Infrastructure.Models;
using TuanKietBranchFlow.Infrastructure.Repositories.Branches;
using TuanKietBranchFlow.Infrastructure.Repositories.Inventory;
using TuanKietBranchFlow.Infrastructure.UnitOfWork;

namespace TuanKietBranchFlow.Tests.Application.Inventory;

public class InventoryServiceTests
{
    private readonly Mock<IInventoryRepository> _inventory = new Mock<IInventoryRepository>();
    private readonly Mock<IBranchRepository> _branches = new Mock<IBranchRepository>();
    private readonly Mock<IUnitOfWork> _unitOfWork = new Mock<IUnitOfWork>();
    private InventoryService Service => new(_inventory.Object, _branches.Object, _unitOfWork.Object);

    [Theory]
    [InlineData(null, null, "UNCONFIGURED")]
    [InlineData(0, 0, "OUT")]
    [InlineData(0, 5, "OUT")]
    [InlineData(1, 0, "AVAILABLE")]
    [InlineData(5, 5, "LOW")]
    [InlineData(4, 5, "LOW")]
    [InlineData(6, 5, "AVAILABLE")]
    public void StockStatus_DistinguishesUnconfiguredAndZero(
        int? quantity,
        int? threshold,
        string expected)
    {
        Assert.Equal(expected, InventoryService.GetStockStatus(quantity, threshold));
    }

    [Theory]
    [InlineData("0", true)]
    [InlineData("1.123", true)]
    [InlineData("-1", false)]
    [InlineData("1.1234", false)]
    [InlineData("999999999999999.999", true)]
    [InlineData("1000000000000000", false)]
    public void Threshold_RejectsNegativeAndPrecisionLoss(
        string input,
        bool expected)
    {
        decimal value = decimal.Parse(input, System.Globalization.CultureInfo.InvariantCulture);
        Assert.Equal(expected, InventoryService.IsValidThreshold(value));
    }

    [Fact]
    public async Task GetStock_OutsideBranch_DoesNotReadInventory()
    {
        SetBranch(false);
        InventoryResultDTO result = await Service.GetStockAsync(10, "ADMIN", new InventoryQueryDTO
        {
            BranchId = 1
        });
        Assert.Equal("Forbidden", result.ErrorCode);
        _inventory.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetStock_OwnerCanReadInactiveBranch_WithoutAssignment()
    {
        SetBranch(false, false);
        _inventory.Setup(repository => repository.GetStockAsync(1, null, null, null, 1, 20)).ReturnsAsync(new InventoryStockPage
        {
            ConfiguredCount = 2,
            UnconfiguredCount = 1
        });
        InventoryResultDTO result = await Service.GetStockAsync(10, "OWNER", new InventoryQueryDTO
        {
            BranchId = 1
        });
        Assert.Null(result.ErrorCode);
        Assert.False(result.Inventory!.CanConfigure);
        Assert.Equal(2, result.Inventory.ConfiguredCount);
        _branches.Verify(repository => repository.HasActiveAssignmentAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DateOnly>()), Times.Never);
        _unitOfWork.Verify(repository => repository.SaveChangesAsync(), Times.Never);
    }

    [Theory]
    [InlineData("INVALID", 1)]
    [InlineData("OUT", 0)]
    [InlineData("OUT", 10001)]
    public async Task GetStock_InvalidFilter_StopsBeforeDatabase(
        string status,
        int page)
    {
        InventoryResultDTO result = await Service.GetStockAsync(10, "ADMIN", new InventoryQueryDTO
        {
            BranchId = 1,
            StockStatus = status,
            Page = page
        });
        Assert.Equal("Invalid", result.ErrorCode);
        _branches.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Configure_StartsAtZero_AndSavesOnlyConfiguration()
    {
        SetBranch(true);
        _inventory.Setup(repository => repository.GetIngredientAsync(2)).ReturnsAsync(new Ingredient
        {
            Id = 2,
            UnitId = 3,
            IsActive = true
        });
        _inventory.Setup(repository => repository.GetUnitAsync(3)).ReturnsAsync(new IngredientUnit
        {
            Id = 3,
            IsActive = true
        });
        InventoryResultDTO result = await Service.ConfigureAsync(
            10,
            new InventoryConfigurationRequestDTO
            {
                BranchId = 1,
                IngredientId = 2,
                WarningThreshold = 1.234m
            });
        Assert.Null(result.ErrorCode);
        _inventory.Verify(repository => repository.AddAsync(It.Is<BranchIngredient>(item => item.Quantity == 0 && item.WarningThreshold == 1.234m && item.BranchId == 1)), Times.Once);
        _unitOfWork.Verify(repository => repository.CommitTransactionAsync(), Times.Once);
    }

    [Fact]
    public async Task Configure_InactiveBranch_RejectsWrite()
    {
        SetBranch(true, false);
        InventoryResultDTO result = await Service.ConfigureAsync(10, new InventoryConfigurationRequestDTO
        {
            BranchId = 1,
            IngredientId = 2
        });
        Assert.Equal("Conflict", result.ErrorCode);
        _inventory.Verify(repository => repository.AddAsync(It.IsAny<BranchIngredient>()), Times.Never);
    }

    [Fact]
    public async Task Configure_Duplicate_ReturnsConflict()
    {
        SetBranch(true);
        _inventory.Setup(repository => repository.GetIngredientAsync(2)).ReturnsAsync(new Ingredient
        {
            Id = 2,
            UnitId = 3,
            IsActive = true
        });
        _inventory.Setup(repository => repository.GetUnitAsync(3)).ReturnsAsync(new IngredientUnit
        {
            Id = 3,
            IsActive = true
        });
        _inventory.Setup(repository => repository.ConfigurationExistsAsync(1, 2)).ReturnsAsync(true);
        InventoryResultDTO result = await Service.ConfigureAsync(10, new InventoryConfigurationRequestDTO
        {
            BranchId = 1,
            IngredientId = 2
        });
        Assert.Equal("Conflict", result.ErrorCode);
        _unitOfWork.Verify(repository => repository.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task Threshold_StaleVersion_DoesNotSave()
    {
        SetBranch(true);
        _inventory.Setup(repository => repository.GetConfigurationAsync(5)).ReturnsAsync(new BranchIngredient
        {
            Id = 5,
            BranchId = 1,
            RowVersion = new byte[8]
        });
        InventoryResultDTO result = await Service.UpdateThresholdAsync(
            10,
            5,
            new InventoryThresholdRequestDTO
            {
                ExpectedRowVersion = Enumerable.Repeat((byte)1, 8).ToArray()
            });
        Assert.Equal("Conflict", result.ErrorCode);
        _unitOfWork.Verify(repository => repository.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task Threshold_SaveConflict_ReturnsConflictAndRollsBack()
    {
        SetBranch(true);
        BranchIngredient configuration = new BranchIngredient()

        {

            Id = 5,
            BranchId = 1,
            Quantity = 99,
            RowVersion = new byte[8]

        };
        _inventory.Setup(repository => repository.GetConfigurationAsync(5)).ReturnsAsync(configuration);
        _unitOfWork.Setup(repository => repository.SaveChangesAsync()).ThrowsAsync(new DbUpdateConcurrencyException());
        InventoryResultDTO result = await Service.UpdateThresholdAsync(
            10,
            5,
            new InventoryThresholdRequestDTO
            {
                WarningThreshold = 2,
                ExpectedRowVersion = new byte[8]
            });
        Assert.Equal("Conflict", result.ErrorCode);
        Assert.Equal(99, configuration.Quantity);
        _unitOfWork.Verify(repository => repository.RollbackTransactionAsync(), Times.Once);
        _unitOfWork.Verify(repository => repository.CommitTransactionAsync(), Times.Never);
    }

    [Fact]
    public async Task Unit_DisableWithActiveIngredients_Rejects()
    {
        _inventory.Setup(repository => repository.GetUnitAsync(3)).ReturnsAsync(new IngredientUnit
        {
            Id = 3,
            IsActive = true
        });
        _inventory.Setup(repository => repository.UnitHasActiveIngredientsAsync(3)).ReturnsAsync(true);
        Assert.Equal("Conflict", (await Service.UpdateUnitStatusAsync(3, false)).ErrorCode);
        _unitOfWork.Verify(repository => repository.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task Unit_RenameUsedUnit_Rejects()
    {
        _inventory.Setup(repository => repository.GetUnitAsync(3)).ReturnsAsync(new IngredientUnit
        {
            Id = 3,
            Name = "kg"
        });
        _inventory.Setup(repository => repository.UnitIsUsedAsync(3)).ReturnsAsync(true);
        Assert.Equal("Conflict", (await Service.UpdateUnitAsync(3, "lít")).ErrorCode);
    }

    [Fact]
    public async Task Ingredient_ChangeUsedUnit_Rejects()
    {
        _inventory.Setup(repository => repository.GetIngredientAsync(2)).ReturnsAsync(new Ingredient
        {
            Id = 2,
            Name = "Trà",
            UnitId = 3
        });
        _inventory.Setup(repository => repository.IngredientIsUsedAsync(2)).ReturnsAsync(true);
        Assert.Equal("Conflict", (await Service.UpdateIngredientAsync(2, "Trà", 4)).ErrorCode);
    }

    [Fact]
    public async Task Ingredient_ReactivateWithInactiveUnit_Rejects()
    {
        _inventory.Setup(repository => repository.GetIngredientAsync(2)).ReturnsAsync(new Ingredient
        {
            Id = 2,
            Name = "Trà",
            UnitId = 3
        });
        _inventory.Setup(repository => repository.GetUnitAsync(3)).ReturnsAsync(new IngredientUnit
        {
            Id = 3,
            IsActive = false
        });
        Assert.Equal("Conflict", (await Service.UpdateIngredientStatusAsync(2, true)).ErrorCode);
    }

    [Fact]
    public async Task Ingredient_NameTrimmed_WithoutChangingStatus()
    {
        Ingredient ingredient = new Ingredient()

        {

            Id = 2,
            Name = "Trà",
            UnitId = 3,
            IsActive = false

        };
        _inventory.Setup(repository => repository.GetIngredientAsync(2)).ReturnsAsync(ingredient);
        _inventory.Setup(repository => repository.GetUnitAsync(3)).ReturnsAsync(new IngredientUnit
        {
            Id = 3,
            IsActive = true
        });
        Assert.Null((await Service.UpdateIngredientAsync(2, "  Trà mới  ", 3)).ErrorCode);
        Assert.Equal("Trà mới", ingredient.Name);
        Assert.False(ingredient.IsActive);
    }

    private void SetBranch(
        bool assigned,
        bool active = true)
    {
        _branches.Setup(repository => repository.GetNotDeletedByIdAsync(1)).ReturnsAsync(new Branch
        {
            Id = 1,
            IsActive = active
        });
        _branches.Setup(repository => repository.HasActiveAssignmentAsync(10, 1, It.IsAny<DateOnly>())).ReturnsAsync(assigned);
    }
}
