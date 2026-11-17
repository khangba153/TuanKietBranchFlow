using Moq;
using TuanKietBranchFlow.Application.DTOs.Employees.Transfers;
using TuanKietBranchFlow.Application.Services.Employees;
using TuanKietBranchFlow.Infrastructure.Models;
using TuanKietBranchFlow.Infrastructure.Repositories.Auth;
using TuanKietBranchFlow.Infrastructure.Repositories.Branches;
using TuanKietBranchFlow.Infrastructure.Repositories.Employees;
using TuanKietBranchFlow.Infrastructure.Repositories.Users;
using TuanKietBranchFlow.Infrastructure.UnitOfWork;

namespace TuanKietBranchFlow.Tests.Application.Employees;

public class EmployeeTransferServiceTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Create_SelectsApprovedWorkflowWithoutRevokingPendingSessions(bool immediate)
    {
        Fixture fixture = new Fixture();
        fixture.Branches.Setup(repository => repository.HasActiveAssignmentAsync(1, 3, fixture.Today)).ReturnsAsync(immediate);

        EmployeeTransferResultDTO result = await fixture.Service.CreateAsync(1, fixture.Input(immediate));

        Assert.Null(result.ErrorCode);
        Assert.Equal(immediate ? "TRANSFERRED" : "PENDING", result.Request!.Status);
        fixture.Sessions.Verify(repository => repository.RevokeAllForEmployeeStatusAsync(20, It.IsAny<DateTime>()),
            immediate ? Times.Once() : Times.Never());
        fixture.Assignments.Verify(repository => repository.AddAsync(It.IsAny<UserBranch>()),
            immediate ? Times.Once() : Times.Never());
        Assert.Equal(immediate ? fixture.Today.AddDays(-1) : null, fixture.Source.ActiveTo);
        fixture.Unit.Verify(unit => unit.CommitTransactionAsync(), Times.Once);
    }

    [Fact]
    public async Task Create_RejectsDuplicatePendingRequest()
    {
        Fixture fixture = new Fixture();
        fixture.Transfers.Setup(repository => repository.GetPendingAsync(10)).ReturnsAsync(fixture.Pending);

        EmployeeTransferResultDTO result = await fixture.Service.CreateAsync(1, fixture.Input(false));

        Assert.Equal("Conflict", result.ErrorCode);
        fixture.VerifyNoTransfer();
    }

    [Fact]
    public async Task Create_RejectsForgedImmediateTransferOutsideScope()
    {
        Fixture fixture = new Fixture();

        EmployeeTransferResultDTO result = await fixture.Service.CreateAsync(1, fixture.Input(true));

        Assert.Equal("Conflict", result.ErrorCode);
        fixture.VerifyNoTransfer();
    }

    [Fact]
    public async Task Create_RejectsInactiveTarget()
    {
        Fixture fixture = new Fixture();
        fixture.Target.IsActive = false;

        EmployeeTransferResultDTO result = await fixture.Service.CreateAsync(1, fixture.Input(false));

        Assert.Equal("Invalid", result.ErrorCode);
        fixture.VerifyNoTransfer();
    }

    [Fact]
    public async Task Accept_ClosesSourceCreatesTargetAndRevokesSessions()
    {
        Fixture fixture = new Fixture();

        EmployeeTransferResultDTO result = await fixture.Service.DecideAsync(4, 100, "accept", null);

        Assert.Equal("TRANSFERRED", result.Request!.Status);
        Assert.Equal(fixture.Today, result.Request.EffectiveDate);
        Assert.Equal(fixture.Today.AddDays(-1), fixture.Source.ActiveTo);
        Assert.Equal(4, fixture.Pending.ProcessedByUserId);
        fixture.Assignments.Verify(repository => repository.AddAsync(It.Is<UserBranch>(assignment =>
            assignment.UserId == 20 && assignment.BranchId == 3 && assignment.ActiveFrom == fixture.Today)), Times.Once);
        fixture.Sessions.Verify(repository => repository.RevokeAllForEmployeeStatusAsync(20, It.IsAny<DateTime>()), Times.Once);
    }

    [Theory]
    [InlineData("reject", "Không đủ nhân sự hướng dẫn", "REJECTED", 4)]
    [InlineData("cancel", "Thay đổi kế hoạch", "CANCELLED", 1)]
    public async Task ResolveWithoutTransfer_PreservesAssignmentAndSessions(string action, string reason, string status, int adminId)
    {
        Fixture fixture = new Fixture();

        EmployeeTransferResultDTO result = await fixture.Service.DecideAsync(adminId, 100, action, reason);

        Assert.Equal(status, result.Request!.Status);
        Assert.Equal(reason, result.Request.ResolutionReason);
        fixture.VerifyNoTransfer();
        fixture.Unit.Verify(unit => unit.CommitTransactionAsync(), Times.Once);
    }

    [Fact]
    public async Task Reject_RequiresReasonWithoutOpeningTransaction()
    {
        Fixture fixture = new Fixture();

        EmployeeTransferResultDTO result = await fixture.Service.DecideAsync(4, 100, "reject", " ");

        Assert.Equal("Invalid", result.ErrorCode);
        fixture.Unit.Verify(unit => unit.BeginTransactionAsync(), Times.Never);
    }

    [Theory]
    [InlineData(1, "accept")]
    [InlineData(4, "cancel")]
    public async Task Decision_RequiresCorrectBranchAuthority(int adminId, string action)
    {
        Fixture fixture = new Fixture();

        EmployeeTransferResultDTO result = await fixture.Service.DecideAsync(adminId, 100, action, null);

        Assert.Equal("Forbidden", result.ErrorCode);
        fixture.VerifyNoTransfer();
    }

    [Theory]
    [InlineData("TRANSFERRED")]
    [InlineData("REJECTED")]
    [InlineData("CANCELLED")]
    [InlineData("INVALIDATED")]
    public async Task Decision_CannotProcessTerminalRequestAgain(string status)
    {
        Fixture fixture = new Fixture();
        fixture.Pending.Status = status;

        EmployeeTransferResultDTO result = await fixture.Service.DecideAsync(4, 100, "accept", null);

        Assert.Equal("Conflict", result.ErrorCode);
        fixture.VerifyNoTransfer();
    }

    [Fact]
    public async Task Accept_InvalidatesRequestWhenRequesterLosesSourcePermission()
    {
        Fixture fixture = new Fixture();
        fixture.Branches.Setup(repository => repository.HasActiveAssignmentAsync(1, 2, fixture.Today)).ReturnsAsync(false);

        EmployeeTransferResultDTO result = await fixture.Service.DecideAsync(4, 100, "accept", null);

        Assert.Equal("INVALIDATED", result.Request!.Status);
        fixture.VerifyNoTransfer();
    }

    [Fact]
    public async Task Accept_InvalidatesRequestWhenEmployeeIsInactive()
    {
        Fixture fixture = new Fixture();
        fixture.Employee.User.IsActive = false;

        EmployeeTransferResultDTO result = await fixture.Service.DecideAsync(4, 100, "accept", null);

        Assert.Equal("INVALIDATED", result.Request!.Status);
        fixture.VerifyNoTransfer();
    }

    [Fact]
    public async Task SaveFailure_RollsBackWithoutCommit()
    {
        Fixture fixture = new Fixture();
        fixture.Unit.Setup(unit => unit.SaveChangesAsync()).ThrowsAsync(new InvalidOperationException("Test failure"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.DecideAsync(4, 100, "accept", null));

        fixture.Unit.Verify(unit => unit.RollbackTransactionAsync(), Times.Once);
        fixture.Unit.Verify(unit => unit.ClearTracking(), Times.Once);
        fixture.Unit.Verify(unit => unit.CommitTransactionAsync(), Times.Never);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void AssignmentRules_BlockSameDayFutureAndOverlap(int scenario)
    {
        Fixture fixture = new Fixture();
        if (scenario == 0)
        {
            fixture.Source.ActiveFrom = fixture.Today;
        }
        else
        {
            fixture.Employee.User.UserBranches.Add(new UserBranch
            {
                ActiveFrom = scenario == 1 ? fixture.Today.AddDays(1) : fixture.Today.AddDays(-1)
            });
        }

        Assert.NotNull(EmployeeTransferService.ValidateAssignments(fixture.Employee, fixture.Source, fixture.Today));
    }

    private sealed class Fixture
    {
        public DateOnly Today { get; } = DateOnly.FromDateTime(DateTime.Today);
        public Mock<IEmployeeTransferRepository> Transfers { get; } = new Mock<IEmployeeTransferRepository>();
        public Mock<IEmployeeRepository> Employees { get; } = new Mock<IEmployeeRepository>();
        public Mock<IBranchRepository> Branches { get; } = new Mock<IBranchRepository>();
        public Mock<IUserBranchRepository> Assignments { get; } = new Mock<IUserBranchRepository>();
        public Mock<IAuthSessionRepository> Sessions { get; } = new Mock<IAuthSessionRepository>();
        public Mock<IUnitOfWork> Unit { get; } = new Mock<IUnitOfWork>();
        public Branch Target { get; } = new Branch { Id = 3, IsActive = true, Code = "B", Name = "Chi nhánh B" };
        public EmployeeProfile Employee { get; }
        public UserBranch Source { get; }
        public EmployeeTransferRequest Pending { get; }
        public EmployeeTransferService Service { get; }

        public Fixture()
        {
            Branch sourceBranch = new Branch { Id = 2, IsActive = true, Code = "A", Name = "Chi nhánh A" };
            Source = new UserBranch { Id = 50, UserId = 20, BranchId = 2, Branch = sourceBranch, ActiveFrom = Today.AddDays(-30) };
            Employee = new EmployeeProfile
            {
                Id = 10, UserId = 20, EmployeeCode = "NV-TEST",
                User = new AppUser { Id = 20, IsActive = true, FullName = "Nhân viên thử" }
            };
            Employee.User.UserBranches.Add(Source);
            Pending = new EmployeeTransferRequest
            {
                Id = 100, EmployeeId = 10, SourceAssignmentId = 50, SourceBranchId = 2, TargetBranchId = 3,
                RequestedByUserId = 1, Status = "PENDING", EmployeeCode = "NV-TEST", EmployeeName = "Nhân viên thử",
                SourceBranchName = "Chi nhánh A", TargetBranchName = "Chi nhánh B", RequestedByName = "Admin nguồn"
            };
            Mock<IUserRepository> users = new Mock<IUserRepository>();
            users.Setup(repository => repository.GetProfileByIdAsync(It.IsAny<int>())).ReturnsAsync((int id) =>
                new AppUser { Id = id, IsActive = true, FullName = "Admin " + id, Role = new Role { Code = "ADMIN" } });
            Branches.Setup(repository => repository.GetNotDeletedByIdAsync(2)).ReturnsAsync(sourceBranch);
            Branches.Setup(repository => repository.GetNotDeletedByIdAsync(3)).ReturnsAsync(Target);
            Branches.Setup(repository => repository.HasActiveAssignmentAsync(1, 2, Today)).ReturnsAsync(true);
            Branches.Setup(repository => repository.HasActiveAssignmentAsync(4, 3, Today)).ReturnsAsync(true);
            Employees.Setup(repository => repository.LockAccountForStatusChangeAsync(10)).ReturnsAsync(1);
            Employees.Setup(repository => repository.GetByIdWithUserAndBranchesAsync(10)).ReturnsAsync(Employee);
            Transfers.Setup(repository => repository.GetEmployeeIdAsync(100)).ReturnsAsync(10);
            Transfers.Setup(repository => repository.GetByIdAsync(It.IsAny<object[]>())).ReturnsAsync(Pending);
            Service = new EmployeeTransferService(Transfers.Object, Employees.Object, Branches.Object,
                Assignments.Object, users.Object, Sessions.Object, Unit.Object);
        }

        public EmployeeTransferCreateDTO Input(bool immediate)
        {
            return new EmployeeTransferCreateDTO
            {
                EmployeeId = 10, SourceBranchId = 2, TargetBranchId = 3, TransferImmediately = immediate
            };
        }

        public void VerifyNoTransfer()
        {
            Sessions.Verify(repository => repository.RevokeAllForEmployeeStatusAsync(It.IsAny<int>(), It.IsAny<DateTime>()), Times.Never);
            Assignments.Verify(repository => repository.AddAsync(It.IsAny<UserBranch>()), Times.Never);
            Assert.Null(Source.ActiveTo);
        }
    }
}
