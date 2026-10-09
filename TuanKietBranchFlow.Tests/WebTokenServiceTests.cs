using Microsoft.Extensions.Configuration;
using Moq;
using TuanKietBranchFlow.Web.Models;
using TuanKietBranchFlow.Web.Services;
using System.Net;
using System.Net.Http.Json;
using Moq.Protected;
using TuanKietBranchFlow.Application.DTOs.Auth;

namespace TuanKietBranchFlow.Tests;

public class WebTokenServiceTests
{
    // Token còn hạn được dùng lại, không gọi API refresh
    [Fact]
    public async Task GetAccessTokenAsync_ValidToken_ReturnsTokenWithoutCallingApi()
    {
        IConfiguration configuration =
            new ConfigurationBuilder().Build();

        WebAuthSessionStore store =
            new WebAuthSessionStore(configuration);

        Mock<IHttpClientFactory> factoryMock =
            new Mock<IHttpClientFactory>();

        WebAuthSession session = new WebAuthSession
        {
            Id = Guid.NewGuid(),
            UserId = 1,
            AccessToken = "access-token-for-unit-test",
            RefreshToken = "refresh-token-for-unit-test",
            AccessTokenExpiresAt = DateTime.UtcNow.AddMinutes(5),
            RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(1)
        };

        Assert.True(store.TryAdd(session));

        WebTokenService service =
            new WebTokenService(store, factoryMock.Object);

        string? result =
            await service.GetAccessTokenAsync(session.Id);

        Assert.Equal(session.AccessToken, result);

        factoryMock.Verify(
            factory => factory.CreateClient(It.IsAny<string>()),
            Times.Never);
    }

    // Không tìm thấy phiên thì không gọi API refresh
    [Fact]
    public async Task GetAccessTokenAsync_MissingSession_ReturnsNullWithoutCallingApi()
    {
        IConfiguration configuration =
            new ConfigurationBuilder().Build();

        WebAuthSessionStore store =
            new WebAuthSessionStore(configuration);

        Mock<IHttpClientFactory> factoryMock =
            new Mock<IHttpClientFactory>();

        WebTokenService service =
            new WebTokenService(store, factoryMock.Object);

        string? result =
            await service.GetAccessTokenAsync(Guid.NewGuid());

        Assert.Null(result);

        factoryMock.Verify(
            factory => factory.CreateClient(It.IsAny<string>()),
            Times.Never);
    }

    // Access token hết hạn thì refresh và cập nhật cặp token trong kho RAM.
    [Fact]
    public async Task GetAccessTokenAsync_ExpiredToken_RefreshesAndUpdatesSession()
    {
        IConfiguration configuration =
            new ConfigurationBuilder().Build();

        WebAuthSessionStore store =
            new WebAuthSessionStore(configuration);

        DateTime now = DateTime.UtcNow;

        WebAuthSession session = new WebAuthSession
        {
            Id = Guid.NewGuid(),
            UserId = 1,
            AccessToken = "old-access-token-for-test",
            RefreshToken = new string('a', 44),
            AccessTokenExpiresAt = now.AddMinutes(-1),
            RefreshTokenExpiresAt = now.AddDays(1)
        };

        Assert.True(store.TryAdd(session));

        LoginResponseDTO tokenResponse = new LoginResponseDTO
        {
            AccessToken = "new-access-token-for-test",
            RefreshToken = new string('b', 44),
            AccessTokenExpiresAt = now.AddMinutes(15),
            RefreshTokenExpiresAt = session.RefreshTokenExpiresAt
        };

        // Giả lập response của API, không gửi request ra mạng.
        Mock<HttpMessageHandler> handlerMock =
            new Mock<HttpMessageHandler>();

        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(tokenResponse)
            });

        using HttpClient client = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("https://unit-test.invalid/")
        };

        Mock<IHttpClientFactory> factoryMock =
            new Mock<IHttpClientFactory>();

        factoryMock
            .Setup(factory => factory.CreateClient("BranchFlowApi"))
            .Returns(client);

        WebTokenService service =
            new WebTokenService(store, factoryMock.Object);

        string? result =
            await service.GetAccessTokenAsync(session.Id);

        // Token trả về và dữ liệu trong RAM đều phải được cập nhật.
        Assert.Equal(tokenResponse.AccessToken, result);
        Assert.Equal(tokenResponse.AccessToken, session.AccessToken);
        Assert.Equal(tokenResponse.RefreshToken, session.RefreshToken);
        Assert.Equal(
            tokenResponse.AccessTokenExpiresAt,
            session.AccessTokenExpiresAt);

        // Refresh không kéo dài thời hạn phiên ban đầu.
        Assert.Equal(
            now.AddDays(1),
            session.RefreshTokenExpiresAt);

        Assert.Same(session, store.GetById(session.Id));
    }

    // API từ chối refresh thì không giữ phiên Web để tiếp tục sử dụng.
    [Fact]
    public async Task GetAccessTokenAsync_RefreshRejected_RemovesSession()
    {
        IConfiguration configuration =
            new ConfigurationBuilder().Build();

        WebAuthSessionStore store =
            new WebAuthSessionStore(configuration);

        WebAuthSession session = new WebAuthSession
        {
            Id = Guid.NewGuid(),
            UserId = 1,
            AccessToken = "expired-access-token-for-test",
            RefreshToken = new string('a', 44),
            AccessTokenExpiresAt = DateTime.UtcNow.AddMinutes(-1),
            RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(1)
        };

        Assert.True(store.TryAdd(session));

        // Giả lập API từ chối refresh token hoặc phiên.
        Mock<HttpMessageHandler> handlerMock =
            new Mock<HttpMessageHandler>();

        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(
                new HttpResponseMessage(HttpStatusCode.Unauthorized));

        using HttpClient client = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("https://unit-test.invalid/")
        };

        Mock<IHttpClientFactory> factoryMock =
            new Mock<IHttpClientFactory>();

        factoryMock
            .Setup(factory => factory.CreateClient("BranchFlowApi"))
            .Returns(client);

        WebTokenService service =
            new WebTokenService(store, factoryMock.Object);

        string? result =
            await service.GetAccessTokenAsync(session.Id);

        Assert.Null(result);
        Assert.Null(store.GetById(session.Id));
    }

    // Hai lời gọi cùng phiên chỉ refresh một lần và cùng nhận token mới.
    [Fact]
    public async Task GetAccessTokenAsync_ConcurrentCalls_RefreshesOnlyOnce()
    {
        IConfiguration configuration =
            new ConfigurationBuilder().Build();

        WebAuthSessionStore store =
            new WebAuthSessionStore(configuration);

        DateTime now = DateTime.UtcNow;

        WebAuthSession session = new WebAuthSession
        {
            Id = Guid.NewGuid(),
            UserId = 1,
            AccessToken = "expired-access-token-for-test",
            RefreshToken = new string('a', 44),
            AccessTokenExpiresAt = now.AddMinutes(-1),
            RefreshTokenExpiresAt = now.AddDays(1)
        };

        Assert.True(store.TryAdd(session));

        LoginResponseDTO tokenResponse = new LoginResponseDTO
        {
            AccessToken = "new-access-token-for-test",
            RefreshToken = new string('b', 44),
            AccessTokenExpiresAt = now.AddMinutes(15),
            RefreshTokenExpiresAt = session.RefreshTokenExpiresAt
        };

        // Giữ response API ở trạng thái chờ cho đến khi test mở cổng.
        TaskCompletionSource<HttpResponseMessage> responseGate =
            new TaskCompletionSource<HttpResponseMessage>(
                TaskCreationOptions.RunContinuationsAsynchronously);

        Mock<HttpMessageHandler> handlerMock =
            new Mock<HttpMessageHandler>();

        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Returns(() => responseGate.Task);

        using HttpClient client = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("https://unit-test.invalid/")
        };

        Mock<IHttpClientFactory> factoryMock =
            new Mock<IHttpClientFactory>();

        factoryMock
            .Setup(factory => factory.CreateClient("BranchFlowApi"))
            .Returns(client);

        // Hai service khác nhau nhưng dùng chung kho và phiên.
        WebTokenService serviceA =
            new WebTokenService(store, factoryMock.Object);

        WebTokenService serviceB =
            new WebTokenService(store, factoryMock.Object);

        // A chờ API; B phải chờ khóa của phiên.
        Task<string?> taskA = serviceA.GetAccessTokenAsync(session.Id);
        Task<string?> taskB = serviceB.GetAccessTokenAsync(session.Id);

        Assert.False(taskA.IsCompleted);
        Assert.False(taskB.IsCompleted);

        handlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>());

        // Cho API giả trả cặp token mới để A tiếp tục và nhả khóa.
        responseGate.SetResult(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(tokenResponse)
            });

        // Giới hạn thời gian chờ để test không bị treo nếu khóa có lỗi.
        string?[] results = await Task.WhenAll(taskA, taskB)
            .WaitAsync(TimeSpan.FromSeconds(10));

        Assert.Equal(tokenResponse.AccessToken, results[0]);
        Assert.Equal(tokenResponse.AccessToken, results[1]);
        Assert.Equal(tokenResponse.RefreshToken, session.RefreshToken);
        Assert.Same(session, store.GetById(session.Id));

        // Sau khi cả hai hoàn tất, vẫn chỉ có một request refresh.
        handlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>());
    }
}