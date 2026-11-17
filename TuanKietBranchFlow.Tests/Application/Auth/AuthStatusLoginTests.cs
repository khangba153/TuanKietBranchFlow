using Microsoft.AspNetCore.Identity;
using Moq;
using TuanKietBranchFlow.Application.DTOs.Auth;
using TuanKietBranchFlow.Application.Services.Auth;
using TuanKietBranchFlow.Infrastructure.Models;
using TuanKietBranchFlow.Infrastructure.Repositories.Auth;
using TuanKietBranchFlow.Infrastructure.Repositories.Users;
using TuanKietBranchFlow.Infrastructure.UnitOfWork;

namespace TuanKietBranchFlow.Tests.Application.Auth;

public class AuthStatusLoginTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public async Task Login_RequiresUnchangedActiveAccountLockBeforeCreatingSession(int lockedRows)
    {
        AppUser user = new AppUser
        {
            Id = 20,
            Username = "test",
            PasswordHash = "testhash",
            FullName = "Test",
            IsActive = true,
            UpdatedAt = DateTime.UtcNow,
            Role = new Role { Code = "EMPLOYEE", Name = "Employee" }
        };
        Mock<IUserRepository> users = new Mock<IUserRepository>();
        users.Setup(repository => repository.GetByUsernameWithRoleAsync("test")).ReturnsAsync(user);
        users.Setup(repository => repository.LockActiveAccountForLoginAsync(20, user.UpdatedAt)).ReturnsAsync(lockedRows);
        Mock<IPasswordHasher<AppUser>> hasher = new Mock<IPasswordHasher<AppUser>>();
        hasher.Setup(service => service.VerifyHashedPassword(user, "testhash", "testpassword"))
            .Returns(PasswordVerificationResult.Success);
        Mock<IAuthSessionRepository> sessions = new Mock<IAuthSessionRepository>();
        Mock<IRefreshTokenRepository> tokens = new Mock<IRefreshTokenRepository>();
        Mock<IUnitOfWork> unitOfWork = new Mock<IUnitOfWork>();
        AuthService service = new AuthService(users.Object, hasher.Object,
            new JwtTokenService(new string('a', 64), "unit-test", "unit-test", 15),
            sessions.Object, tokens.Object, unitOfWork.Object);

        LoginResponseDTO? result = await service.LoginAsync(new LoginRequestDTO
        {
            Username = "test",
            Password = "testpassword"
        });

        unitOfWork.Verify(unit => unit.BeginTransactionAsync(), Times.Once);
        if (lockedRows == 0)
        {
            Assert.Null(result);
            sessions.Verify(repository => repository.AddAsync(It.IsAny<AuthSession>()), Times.Never);
            tokens.Verify(repository => repository.AddAsync(It.IsAny<RefreshToken>()), Times.Never);
            unitOfWork.Verify(unit => unit.SaveChangesAsync(), Times.Never);
            unitOfWork.Verify(unit => unit.RollbackTransactionAsync(), Times.Once);
            unitOfWork.Verify(unit => unit.ClearTracking(), Times.Once);
        }
        else
        {
            Assert.NotNull(result);
            sessions.Verify(repository => repository.AddAsync(It.IsAny<AuthSession>()), Times.Once);
            tokens.Verify(repository => repository.AddAsync(It.IsAny<RefreshToken>()), Times.Once);
            unitOfWork.Verify(unit => unit.SaveChangesAsync(), Times.Once);
            unitOfWork.Verify(unit => unit.CommitTransactionAsync(), Times.Once);
        }
    }
}
