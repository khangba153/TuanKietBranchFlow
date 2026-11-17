using Microsoft.AspNetCore.Identity;
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

public class EmployeeStatusServiceTests
{
    [Fact]
    public async Task Deactivate_ClosesAssignmentAndRevokesSessionsBeforeCommit()
    {
        StatusFixture fixture = new StatusFixture();

        EmployeeStatusUpdateResultDTO result = await fixture.UpdateAsync(false);

        Assert.NotNull(result.Employee);
        Assert.False(fixture.Employee.User.IsActive);
        Assert.Equal(fixture.Today, fixture.Employee.LeaveDate);
        Assert.Equal(fixture.Today.AddDays(-1), Assert.Single(fixture.Employee.User.UserBranches).ActiveTo);
        fixture.Sessions.Verify(repository => repository.RevokeAllForEmployeeStatusAsync(20, It.IsAny<DateTime>()), Times.Once);
        fixture.Assignments.Verify(repository => repository.AddAsync(It.IsAny<UserBranch>()), Times.Never);
        Assert.Equal(new[] { "begin", "lock", "revoke", "save", "commit" }, fixture.Operations);
    }

    [Fact]
    public async Task Reactivate_PreservesHistoryAndReturnsNewAssignmentOnceAfterRelationshipFixup()
    {
        StatusFixture fixture = new StatusFixture();
        fixture.Employee.User.IsActive = false;
        fixture.Employee.LeaveDate = fixture.Today.AddDays(-10);
        UserBranch oldAssignment = Assert.Single(fixture.Employee.User.UserBranches);
        oldAssignment.ActiveTo = fixture.Today.AddDays(-11);
        fixture.Assignments.Setup(repository => repository.AddAsync(It.IsAny<UserBranch>()))
            .Callback<UserBranch>(assignment => fixture.Employee.User.UserBranches.Add(assignment))
            .Returns(Task.CompletedTask);

        EmployeeStatusUpdateResultDTO result = await fixture.UpdateAsync(true);

        Assert.True(result.Employee!.IsActive);
        Assert.Null(result.Employee.LeaveDate);
        Assert.Equal(fixture.Today.AddDays(-11), oldAssignment.ActiveTo);
        Assert.Equal(2, result.Employee.BranchHistory.Count);
        Assert.Single(result.Employee.BranchHistory, history => history.ActiveFrom == fixture.Today);
        fixture.Sessions.Verify(repository => repository.RevokeAllForEmployeeStatusAsync(20, It.IsAny<DateTime>()), Times.Once);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(1)]
    public async Task WrongEffectiveDate_RollsBackWithoutSaving(int offset)
    {
        StatusFixture fixture = new StatusFixture();

        EmployeeStatusUpdateResultDTO result = await fixture.Service.UpdateEmployeeStatusAsync(
            1, 10, 2, new EmployeeStatusUpdateDTO { IsActive = false, EffectiveDate = fixture.Today.AddDays(offset) });

        Assert.False(result.IsEffectiveDateValid);
        fixture.VerifyRejected();
    }

    [Fact]
    public async Task AssignmentStartedToday_CannotDeactivate()
    {
        StatusFixture fixture = new StatusFixture();
        Assert.Single(fixture.Employee.User.UserBranches).ActiveFrom = fixture.Today;

        EmployeeStatusUpdateResultDTO result = await fixture.UpdateAsync(false);

        Assert.False(result.IsEffectiveDateValid);
        fixture.VerifyRejected();
    }

    [Fact]
    public async Task FutureAssignment_PreventsStatusChange()
    {
        StatusFixture fixture = new StatusFixture();
        fixture.Employee.User.UserBranches.Add(new UserBranch
        {
            UserId = 20,
            BranchId = 3,
            ActiveFrom = fixture.Today.AddDays(3)
        });

        EmployeeStatusUpdateResultDTO result = await fixture.UpdateAsync(false);

        Assert.True(result.HasFutureAssignment);
        fixture.VerifyRejected();
    }

    [Fact]
    public async Task SameStatus_ReturnsConflictWithoutRevokingSessions()
    {
        StatusFixture fixture = new StatusFixture();

        EmployeeStatusUpdateResultDTO result = await fixture.UpdateAsync(true);

        Assert.True(result.IsSameStatus);
        fixture.VerifyRejected();
    }

    [Fact]
    public async Task EmployeeOutsideBranch_CannotChangeStatus()
    {
        StatusFixture fixture = new StatusFixture();
        Assert.Single(fixture.Employee.User.UserBranches).BranchId = 3;

        EmployeeStatusUpdateResultDTO result = await fixture.UpdateAsync(false);

        Assert.False(result.IsEmployeeFound);
        fixture.VerifyRejected();
    }

    [Fact]
    public async Task MissingAccountLock_DoesNotReadOrChangeEmployee()
    {
        StatusFixture fixture = new StatusFixture();
        fixture.Employees.Setup(repository => repository.LockAccountForStatusChangeAsync(10)).ReturnsAsync(0);

        EmployeeStatusUpdateResultDTO result = await fixture.UpdateAsync(false);

        Assert.False(result.IsEmployeeFound);
        fixture.Employees.Verify(repository => repository.GetByIdWithUserAndBranchesAsync(10), Times.Never);
        fixture.VerifyRejected();
    }

    [Fact]
    public async Task RevocationFailure_RollsBackAndClearsTracking()
    {
        StatusFixture fixture = new StatusFixture();
        fixture.Sessions.Setup(repository => repository.RevokeAllForEmployeeStatusAsync(20, It.IsAny<DateTime>()))
            .ThrowsAsync(new InvalidOperationException("Test failure"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.UpdateAsync(false));

        fixture.UnitOfWork.Verify(unit => unit.RollbackTransactionAsync(), Times.Once);
        fixture.UnitOfWork.Verify(unit => unit.ClearTracking(), Times.Once);
        fixture.UnitOfWork.Verify(unit => unit.CommitTransactionAsync(), Times.Never);
        fixture.UnitOfWork.Verify(unit => unit.SaveChangesAsync(), Times.Never);
    }

    private sealed class StatusFixture
    {
        public DateOnly Today { get; } = DateOnly.FromDateTime(DateTime.Today);
        public Mock<IEmployeeRepository> Employees { get; } = new Mock<IEmployeeRepository>();
        public Mock<IUserBranchRepository> Assignments { get; } = new Mock<IUserBranchRepository>();
        public Mock<IAuthSessionRepository> Sessions { get; } = new Mock<IAuthSessionRepository>();
        public Mock<IUnitOfWork> UnitOfWork { get; } = new Mock<IUnitOfWork>();
        public List<string> Operations { get; } = new List<string>();
        public EmployeeProfile Employee { get; }
        public EmployeeService Service { get; }

        public StatusFixture()
        {
            Branch branch = new Branch { Id = 2, Code = "TEST", Name = "Chi nhánh thử", Address = "Địa chỉ thử" };
            Employee = new EmployeeProfile
            {
                Id = 10,
                UserId = 20,
                EmployeeCode = "NV_TEST",
                HireDate = Today.AddDays(-100),
                User = new AppUser { Id = 20, IsActive = true, FullName = "Nhân viên thử", Username = "test", PasswordHash = "test" }
            };
            Employee.User.UserBranches.Add(new UserBranch
            {
                UserId = 20,
                BranchId = 2,
                Branch = branch,
                ActiveFrom = Today.AddDays(-100)
            });
            Mock<IBranchRepository> branches = new Mock<IBranchRepository>();
            branches.Setup(repository => repository.GetNotDeletedByIdAsync(2)).ReturnsAsync(branch);
            branches.Setup(repository => repository.HasActiveAssignmentAsync(1, 2, Today)).ReturnsAsync(true);
            Employees.Setup(repository => repository.LockAccountForStatusChangeAsync(10))
                .Callback(() => Operations.Add("lock")).ReturnsAsync(1);
            Employees.Setup(repository => repository.GetByIdWithUserAndBranchesAsync(10)).ReturnsAsync(Employee);
            Sessions.Setup(repository => repository.RevokeAllForEmployeeStatusAsync(20, It.IsAny<DateTime>()))
                .Callback(() => Operations.Add("revoke")).Returns(Task.CompletedTask);
            UnitOfWork.Setup(unit => unit.BeginTransactionAsync())
                .Callback(() => Operations.Add("begin")).Returns(Task.CompletedTask);
            UnitOfWork.Setup(unit => unit.SaveChangesAsync())
                .Callback(() => Operations.Add("save")).ReturnsAsync(1);
            UnitOfWork.Setup(unit => unit.CommitTransactionAsync())
                .Callback(() => Operations.Add("commit")).Returns(Task.CompletedTask);
            Service = new EmployeeService(Employees.Object, branches.Object, Mock.Of<IUserRepository>(),
                Mock.Of<IRoleRepository>(), Assignments.Object, Mock.Of<IPasswordHasher<AppUser>>(),
                UnitOfWork.Object, Sessions.Object);
        }

        public Task<EmployeeStatusUpdateResultDTO> UpdateAsync(bool isActive)
        {
            return Service.UpdateEmployeeStatusAsync(1, 10, 2,
                new EmployeeStatusUpdateDTO { IsActive = isActive, EffectiveDate = Today });
        }

        public void VerifyRejected()
        {
            Sessions.Verify(repository => repository.RevokeAllForEmployeeStatusAsync(It.IsAny<int>(), It.IsAny<DateTime>()), Times.Never);
            UnitOfWork.Verify(unit => unit.SaveChangesAsync(), Times.Never);
            UnitOfWork.Verify(unit => unit.CommitTransactionAsync(), Times.Never);
            UnitOfWork.Verify(unit => unit.RollbackTransactionAsync(), Times.Once);
        }
    }
}
