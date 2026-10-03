using Moq;
using TuanKietBranchFlow.Application.DTOs.MenuManagement;
using TuanKietBranchFlow.Application.Services;
using TuanKietBranchFlow.Infrastructure.Models;
using TuanKietBranchFlow.Infrastructure.Repositories;
using TuanKietBranchFlow.Infrastructure.UnitOfWork;

namespace TuanKietBranchFlow.Tests;

public class NoteServiceTests
{
    // Dữ liệu sai phải bị từ chối trước khi gọi Repository hoặc lưu
    [Fact]
    public async Task CreateOption_BlankName_DoesNotSave()
    {
        Mock<INoteOptionRepository> options = new Mock<INoteOptionRepository>();
        Mock<INoteGroupRepository> groups = new Mock<INoteGroupRepository>();
        Mock<IUnitOfWork> unit = new Mock<IUnitOfWork>();
        NoteOptionService service = new NoteOptionService(options.Object, groups.Object, unit.Object);
        CreateNoteOptionResultDTO result = await service.CreateNoteOptionAsync(
            new CreateNoteOptionRequestDTO { NoteGroupId = 1, Name = "   " });
        Assert.True(result.IsRequestInvalid);
        groups.Verify(x => x.GetNotDeletedByIdAsync(It.IsAny<int>()), Times.Never);
        unit.Verify(x => x.SaveChangesAsync(), Times.Never);
    }

    // Kiểm tra tên trùng trong đúng nhóm và chuẩn hóa tên trước khi lưu
    [Fact]
    public async Task CreateOption_TrimsName_ChecksDuplicateWithinGroup()
    {
        Mock<INoteOptionRepository> options = new Mock<INoteOptionRepository>();
        Mock<INoteGroupRepository> groups = new Mock<INoteGroupRepository>();
        Mock<IUnitOfWork> unit = new Mock<IUnitOfWork>();
        groups.Setup(x => x.GetNotDeletedByIdAsync(7))
            .ReturnsAsync(new NoteGroup { Id = 7, Name = "Đường" });
        options.Setup(x => x.AddAsync(It.IsAny<NoteOption>()))
            .Callback<NoteOption>(option => option.Id = 10)
            .Returns(Task.CompletedTask);
        NoteOptionService service = new NoteOptionService(options.Object, groups.Object, unit.Object);
        CreateNoteOptionResultDTO result = await service.CreateNoteOptionAsync(
            new CreateNoteOptionRequestDTO { NoteGroupId = 7, Name = "  Ít đường  " });
        Assert.Equal("Ít đường", result.NoteOption!.Name);
        Assert.True(result.NoteOption.IsActive);
        options.Verify(x => x.ExistsByNameAsync("Ít đường", 7), Times.Once);
        unit.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    // Trạng thái false là hợp lệ; bỏ thiếu trạng thái mới bị từ chối
    [Fact]
    public async Task UpdateOption_FalseStatus_UpdatesTrackedEntity()
    {
        Mock<INoteOptionRepository> options = new Mock<INoteOptionRepository>();
        Mock<INoteGroupRepository> groups = new Mock<INoteGroupRepository>();
        Mock<IUnitOfWork> unit = new Mock<IUnitOfWork>();
        NoteOption entity = new NoteOption() { Id = 10, NoteGroupId = 7, Name = "Ít đường", IsActive = true };
        options.Setup(x => x.GetNotDeletedByIdAsync(10)).ReturnsAsync(entity);
        NoteOptionService service = new NoteOptionService(options.Object, groups.Object, unit.Object);
        UpdateNoteOptionResultDTO result = await service.UpdateNoteOptionAsync(10,
            new UpdateNoteOptionRequestDTO { Name = "Ít đường", IsActive = false });
        Assert.False(result.NoteOption!.IsActive);
        Assert.NotNull(entity.UpdatedAt);
        options.Verify(x => x.ExistsByNameExceptIdAsync("Ít đường", 10, 7), Times.Once);
        unit.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    // Xóa nhóm chỉ đánh dấu nhóm, không xóa hoặc đổi trạng thái lựa chọn con
    [Fact]
    public async Task DeleteGroup_PreservesChildren()
    {
        Mock<INoteGroupRepository> groups = new Mock<INoteGroupRepository>();
        Mock<IUnitOfWork> unit = new Mock<IUnitOfWork>();
        NoteOption child = new NoteOption() { Id = 10, Name = "Ít đường", IsActive = true };
        NoteGroup group = new NoteGroup() { Id = 7, Name = "Đường", NoteOptions = new List<NoteOption> { child } };
        groups.Setup(x => x.GetNotDeletedByIdAsync(7)).ReturnsAsync(group);
        NoteGroupService service = new NoteGroupService(groups.Object, unit.Object);
        DeleteNoteGroupResultDTO result = await service.DeleteNoteGroupAsync(7);
        Assert.True(result.IsDeleted);
        Assert.True(group.Deleted);
        Assert.NotNull(group.UpdatedAt);
        Assert.False(child.Deleted);
        Assert.True(child.IsActive);
        unit.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    // Không tìm thấy lựa chọn thì không lưu bất kỳ thay đổi nào
    [Fact]
    public async Task DeleteOption_NotFound_DoesNotSave()
    {
        Mock<INoteOptionRepository> options = new Mock<INoteOptionRepository>();
        Mock<INoteGroupRepository> groups = new Mock<INoteGroupRepository>();
        Mock<IUnitOfWork> unit = new Mock<IUnitOfWork>();
        NoteOptionService service = new NoteOptionService(options.Object, groups.Object, unit.Object);
        DeleteNoteOptionResultDTO result = await service.DeleteNoteOptionAsync(999);
        Assert.True(result.IsNoteOptionNotFound);
        Assert.False(result.IsDeleted);
        unit.Verify(x => x.SaveChangesAsync(), Times.Never);
    }
}
