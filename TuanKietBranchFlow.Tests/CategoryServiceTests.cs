using Moq;
using TuanKietBranchFlow.Application.DTOs.MenuManagement;
using TuanKietBranchFlow.Application.Services;
using TuanKietBranchFlow.Infrastructure.Repositories;
using TuanKietBranchFlow.Infrastructure.UnitOfWork;

namespace TuanKietBranchFlow.Tests;

public class CategoryServiceTests
{
    // Tên chỉ có khoảng trắng phải bị từ chối trước khi truy vấn hoặc lưu
    [Fact]
    public async Task UpdateCategoryAsync_BlankName_ReturnsInvalidWithoutSaving()
    {
        Mock<ICategoryRepository> categoryRepositoryMock =
            new Mock<ICategoryRepository>();

        Mock<IUnitOfWork> unitOfWorkMock =
            new Mock<IUnitOfWork>();

        CategoryService service = new CategoryService(
            categoryRepositoryMock.Object,
            unitOfWorkMock.Object);

        UpdateCategoryRequestDTO request = new UpdateCategoryRequestDTO
        {
            Name = " ",
            IsActive = false
        };

        UpdateCategoryResultDTO result =
            await service.UpdateCategoryAsync(1, request);

        Assert.True(result.IsRequestInvalid);
        Assert.False(result.IsCategoryNotFound);
        Assert.False(result.IsNameDuplicated);
        Assert.Null(result.Category);

        categoryRepositoryMock.Verify(
            repository => repository.GetNotDeletedByIdAsync(
                It.IsAny<int>()),
            Times.Never);

        categoryRepositoryMock.Verify(
            repository => repository.ExistsByNameExceptIdAsync(
                It.IsAny<string>(),
                It.IsAny<int>()),
            Times.Never);

        unitOfWorkMock.Verify(
            unitOfWork => unitOfWork.SaveChangesAsync(),
            Times.Never);
    }
}