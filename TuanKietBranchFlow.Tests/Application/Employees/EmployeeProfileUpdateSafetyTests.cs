using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;
using TuanKietBranchFlow.Application.DTOs.Employees;
using TuanKietBranchFlow.Application.Services.Employees;
using TuanKietBranchFlow.Infrastructure.Models;
using TuanKietBranchFlow.Infrastructure.Repositories.Auth;
using TuanKietBranchFlow.Infrastructure.Repositories.Branches;
using TuanKietBranchFlow.Infrastructure.Repositories.Employees;
using TuanKietBranchFlow.Infrastructure.Repositories.Roles;
using TuanKietBranchFlow.Infrastructure.Repositories.Users;
using TuanKietBranchFlow.Infrastructure.UnitOfWork;

namespace TuanKietBranchFlow.Tests.Application.Employees;

public class EmployeeProfileUpdateSafetyTests
{
    [Fact]
    public async Task Update_LocksBeforeReadingAndDoesNotMarkWholeGraphModified()
    {
        Fixture fixture = new Fixture();
        EmployeeUpdateResultDTO result = await fixture.Service.UpdateEmployeeAsync(1, 10, 2, fixture.Input);

        Assert.NotNull(result.Employee);
        Assert.Equal(new[] { "begin", "lock", "read", "save", "commit" }, fixture.Operations);
        Assert.False(result.Employee.IsActive);
        Assert.Equal(fixture.Today.AddDays(-1), result.Employee.LeaveDate);
        fixture.Users.Verify(repository => repository.Update(It.IsAny<AppUser>()), Times.Never);
        fixture.Employees.Verify(repository => repository.Update(It.IsAny<EmployeeProfile>()), Times.Never);
    }

    [Fact]
    public async Task Update_EmployeeMovedWhileWaitingForLock_ReturnsNotFoundWithoutSaving()
    {
        Fixture fixture = new Fixture();
        fixture.Employees.Setup(repository => repository.GetDetailByIdAndBranchAsync(10, 2, fixture.Today))
            .ReturnsAsync((EmployeeProfile?)null);

        EmployeeUpdateResultDTO result = await fixture.Service.UpdateEmployeeAsync(1, 10, 2, fixture.Input);

        Assert.False(result.IsEmployeeFound);
        fixture.Unit.Verify(unit => unit.RollbackTransactionAsync(), Times.Once);
        fixture.Unit.Verify(unit => unit.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task Update_RechecksAdminScopeAfterLock()
    {
        Fixture fixture = new Fixture();
        fixture.Branches.SetupSequence(repository => repository.HasActiveAssignmentAsync(1, 2, fixture.Today))
            .ReturnsAsync(true).ReturnsAsync(false);

        EmployeeUpdateResultDTO result = await fixture.Service.UpdateEmployeeAsync(1, 10, 2, fixture.Input);

        Assert.False(result.HasAccess);
        fixture.Employees.Verify(repository => repository.GetDetailByIdAndBranchAsync(10, 2, fixture.Today), Times.Never);
        fixture.Unit.Verify(unit => unit.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task Update_UnknownDatabaseFailure_RollsBackAndIsNotReportedAsDuplicate()
    {
        Fixture fixture = new Fixture();
        fixture.Unit.Setup(unit => unit.SaveChangesAsync()).ThrowsAsync(new DbUpdateException("Unknown failure"));

        await Assert.ThrowsAsync<DbUpdateException>(() => fixture.Service.UpdateEmployeeAsync(1, 10, 2, fixture.Input));

        fixture.Unit.Verify(unit => unit.RollbackTransactionAsync(), Times.Once);
        fixture.Unit.Verify(unit => unit.ClearTracking(), Times.Once);
        fixture.Unit.Verify(unit => unit.CommitTransactionAsync(), Times.Never);
    }

    private sealed class Fixture
    {
        public DateOnly Today { get; } = DateOnly.FromDateTime(DateTime.Today);
        public Mock<IEmployeeRepository> Employees { get; } = new Mock<IEmployeeRepository>();
        public Mock<IBranchRepository> Branches { get; } = new Mock<IBranchRepository>();
        public Mock<IUserRepository> Users { get; } = new Mock<IUserRepository>();
        public Mock<IUnitOfWork> Unit { get; } = new Mock<IUnitOfWork>();
        public List<string> Operations { get; } = new List<string>();
        public EmployeeService Service { get; }
        public EmployeeUpdateDTO Input { get; }

        public Fixture()
        {
            Branch branch = new Branch { Id = 2, Name = "Chi nhánh thử", IsActive = true };
            Branches.Setup(repository => repository.GetNotDeletedByIdAsync(2)).ReturnsAsync(branch);
            Branches.Setup(repository => repository.HasActiveAssignmentAsync(1, 2, Today)).ReturnsAsync(true);
            EmployeeProfile employee = new EmployeeProfile
            {
                Id = 10, UserId = 20, EmployeeCode = "TEST", HireDate = Today.AddDays(-30), LeaveDate = Today.AddDays(-1),
                User = new AppUser { Id = 20, FullName = "Tên cũ", IsActive = false }
            };
            Employees.Setup(repository => repository.LockAccountForStatusChangeAsync(10))
                .Callback(() => Operations.Add("lock")).ReturnsAsync(1);
            Employees.Setup(repository => repository.GetDetailByIdAndBranchAsync(10, 2, Today))
                .Callback(() => Operations.Add("read")).ReturnsAsync(employee);
            Unit.Setup(unit => unit.BeginTransactionAsync()).Callback(() => Operations.Add("begin")).Returns(Task.CompletedTask);
            Unit.Setup(unit => unit.SaveChangesAsync()).Callback(() => Operations.Add("save")).ReturnsAsync(1);
            Unit.Setup(unit => unit.CommitTransactionAsync()).Callback(() => Operations.Add("commit")).Returns(Task.CompletedTask);
            Input = new EmployeeUpdateDTO { FullName = "Tên mới", EmployeeCode = "TEST", HireDate = employee.HireDate, BaseSalary = 5000000 };
            Service = new EmployeeService(Employees.Object, Branches.Object, Users.Object, Mock.Of<IRoleRepository>(),
                Mock.Of<IUserBranchRepository>(), Mock.Of<IPasswordHasher<AppUser>>(), Unit.Object,
                Mock.Of<IAuthSessionRepository>(), Mock.Of<IEmployeeTransferRepository>(), Mock.Of<IEmployeeTransferService>());
        }
    }
}
