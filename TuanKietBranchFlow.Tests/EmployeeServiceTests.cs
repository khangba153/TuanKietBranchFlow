using Microsoft.AspNetCore.Identity;
using Moq;
using TuanKietBranchFlow.Application.DTOs.Employees;
using TuanKietBranchFlow.Application.Services;
using TuanKietBranchFlow.Infrastructure.Models;
using TuanKietBranchFlow.Infrastructure.Repositories;
using TuanKietBranchFlow.Infrastructure.UnitOfWork;

namespace TuanKietBranchFlow.Tests;

public class EmployeeServiceTests
{
    // Ngày vào làm ở tương lai phải bị từ chối trước khi truy vấn hoặc lưu.
    [Fact]
    public async Task CreateEmployeeAsync_HireDateInFuture_ReturnsErrorWithoutSaving()
    {
        // Arrange: tạo dependency thay thế, không kết nối database.
        Mock<IEmployeeRepository> employeeRepositoryMock =
            new Mock<IEmployeeRepository>();

        Mock<IBranchRepository> branchRepositoryMock =
            new Mock<IBranchRepository>();

        Mock<IUserRepository> userRepositoryMock =
            new Mock<IUserRepository>();

        Mock<IRoleRepository> roleRepositoryMock =
            new Mock<IRoleRepository>();

        Mock<IUserBranchRepository> userBranchRepositoryMock =
            new Mock<IUserBranchRepository>();

        Mock<IPasswordHasher<AppUser>> passwordHasherMock =
            new Mock<IPasswordHasher<AppUser>>();

        Mock<IUnitOfWork> unitOfWorkMock =
            new Mock<IUnitOfWork>();

        EmployeeService employeeService = new EmployeeService(
            employeeRepositoryMock.Object,
            branchRepositoryMock.Object,
            userRepositoryMock.Object,
            roleRepositoryMock.Object,
            userBranchRepositoryMock.Object,
            passwordHasherMock.Object,
            unitOfWorkMock.Object);

        // Dữ liệu giả chỉ phục vụ test; ngày vào làm nằm trong tương lai.
        EmployeeCreateDTO request = new EmployeeCreateDTO
        {
            Username = "employee_test",
            Password = "TestOnly123!",
            FullName = "Nhân viên kiểm thử",
            EmployeeCode = "NV_TEST",
            HireDate = DateOnly.FromDateTime(DateTime.Today).AddDays(7),
            BaseSalary = 5000000,
            BranchId = 1
        };

        int currentAdminId = 1;

        // Act: gọi method thật của Service.
        EmployeeCreateResultDTO result =
            await employeeService.CreateEmployeeAsync(
                currentAdminId,
                request);

        // Assert: trả đúng lỗi nghiệp vụ và không tạo nhân viên.
        Assert.True(result.IsHireDateInFuture);
        Assert.Null(result.Employee);

        // Request bị từ chối nên không được lưu dữ liệu.
        unitOfWorkMock.Verify(
            unitOfWork => unitOfWork.SaveChangesAsync(),
            Times.Never);

        // Service phải trả sớm, không gọi các dependency còn lại.
        employeeRepositoryMock.VerifyNoOtherCalls();
        branchRepositoryMock.VerifyNoOtherCalls();
        userRepositoryMock.VerifyNoOtherCalls();
        roleRepositoryMock.VerifyNoOtherCalls();
        userBranchRepositoryMock.VerifyNoOtherCalls();
        passwordHasherMock.VerifyNoOtherCalls();
    }
}