using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Moq;
using TuanKietBranchFlow.Application.DTOs.Orders.Management;
using TuanKietBranchFlow.Web.Services.Auth;
using TuanKietBranchFlow.Web.Services.Common;
using TuanKietBranchFlow.Web.Services.Orders;

namespace TuanKietBranchFlow.Tests.Web.Orders;

public class OrderManagementApiServiceTests
{
    [Fact]
    public async Task List_EncodesFiltersAndSendsSessionBearerThroughGet()
    {
        using TestHandler handler = new(HttpStatusCode.OK);
        using HttpClient client = new(handler) { BaseAddress = new Uri("https://unit.test/") };
        var service = CreateService(client);
        var page = await service.GetOrdersAsync(new()
        { BranchId = 2, BusinessDate = new DateOnly(2026, 11, 1), Keyword = " BF &/# ", Status = "NEEDS_REVIEW", Page = 2, PageSize = 20 });
        Assert.Equal(HttpMethod.Get, handler.Method);
        Assert.Equal("Bearer unit-test-token", handler.Authorization);
        Assert.Contains("branchId=2", handler.Url);
        Assert.Contains("businessDate=2026-11-01", handler.Url);
        Assert.Contains("keyword=BF%20%26%2F%23", handler.Url);
        Assert.Contains("status=NEEDS_REVIEW", handler.Url);
        Assert.Contains("page=2&pageSize=20", handler.Url);
        Assert.Empty(page.Items);
        Assert.Equal(1, handler.Calls);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.BadRequest)]
    public async Task List_HttpFailure_PreservesStatusWithoutRetry(HttpStatusCode status)
    {
        using TestHandler handler = new(status);
        using HttpClient client = new(handler) { BaseAddress = new Uri("https://unit.test/") };
        var exception = await Assert.ThrowsAsync<HttpRequestException>(() => CreateService(client).GetOrdersAsync(new()));
        Assert.Equal(status, exception.StatusCode);
        Assert.Equal(1, handler.Calls);
    }

    [Fact]
    public async Task Branches_Unauthorized_PreservesStatus()
    {
        using TestHandler handler = new(HttpStatusCode.Unauthorized);
        using HttpClient client = new(handler) { BaseAddress = new Uri("https://unit.test/") };
        var exception = await Assert.ThrowsAsync<HttpRequestException>(() => CreateService(client).GetBranchesAsync());
        Assert.Equal(HttpStatusCode.Unauthorized, exception.StatusCode);
        Assert.EndsWith("/api/branches/accessible", handler.Url);
    }

    [Fact]
    public async Task List_DeserializesSummaryWithoutSummingVisibleItems()
    {
        using TestHandler handler = new(HttpStatusCode.OK)
        {
            Page = new OrderManagementPageDTO
            {
                TotalCount = 2, Page = 1, PageSize = 20,
                Summary = new OrderManagementSummaryDTO
                { TotalOrders = 80, CompletedOrders = 65, AdjustedOrders = 5, NeedsReviewOrders = 8, CancelledOrders = 2, CompletedOrderValue = 3500000m }
            }
        };
        using HttpClient client = new(handler) { BaseAddress = new Uri("https://unit.test/") };
        var page = await CreateService(client).GetOrdersAsync(new() { Status = "CANCELLED" });
        Assert.Empty(page.Items);
        Assert.Equal(2, page.TotalCount);
        Assert.Equal(80, page.Summary.TotalOrders);
        Assert.Equal(65, page.Summary.CompletedOrders);
        Assert.Equal(5, page.Summary.AdjustedOrders);
        Assert.Equal(8, page.Summary.NeedsReviewOrders);
        Assert.Equal(2, page.Summary.CancelledOrders);
        Assert.Equal(3500000m, page.Summary.CompletedOrderValue);
    }

    [Fact]
    public async Task Detail_NotFound_IsNotConvertedToEmptySuccess()
    {
        using TestHandler handler = new(HttpStatusCode.NotFound);
        using HttpClient client = new(handler) { BaseAddress = new Uri("https://unit.test/") };
        var exception = await Assert.ThrowsAsync<HttpRequestException>(() => CreateService(client).GetDetailAsync(99));
        Assert.Equal(HttpStatusCode.NotFound, exception.StatusCode);
        Assert.EndsWith("/api/order-management/99", handler.Url);
        Assert.Equal(HttpMethod.Get, handler.Method);
    }

    private static OrderManagementApiService CreateService(HttpClient client)
    {
        Guid sessionId = Guid.NewGuid();
        var auth = new Mock<AuthenticationStateProvider>();
        auth.Setup(x => x.GetAuthenticationStateAsync()).ReturnsAsync(new AuthenticationState(
            new ClaimsPrincipal(new ClaimsIdentity(new[]
            { new Claim(WebCookieAuthenticationEvents.WebSessionIdClaim, sessionId.ToString()) }, "TestCookie"))));
        var tokens = new Mock<IWebTokenService>();
        tokens.Setup(x => x.GetAccessTokenAsync(sessionId)).ReturnsAsync("unit-test-token");
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(x => x.CreateClient("BranchFlowApi")).Returns(client);
        return new(new AuthorizedApiService(factory.Object, auth.Object, tokens.Object));
    }

    [Theory]
    [InlineData(HttpStatusCode.OK)]
    [InlineData(HttpStatusCode.Conflict)]
    [InlineData(HttpStatusCode.Forbidden)]
    public async Task ResolveReview_PostsOnceAndDoesNotRetry(HttpStatusCode status)
    {
        using TestHandler handler = new(status);
        using HttpClient client = new(handler) { BaseAddress = new Uri("https://unit.test/") };
        var data = new ResolveOrderReviewRequestDTO { Action = "KEEP", Reason = "Checked", ExpectedReportedAt = DateTime.UtcNow };
        if (status == HttpStatusCode.OK) await CreateService(client).ResolveReviewAsync(9, data);
        else
        {
            var error = await Assert.ThrowsAsync<HttpRequestException>(() => CreateService(client).ResolveReviewAsync(9, data));
            Assert.Equal(status, error.StatusCode);
        }
        Assert.Equal(HttpMethod.Post, handler.Method);
        Assert.EndsWith("/api/order-management/9/review", handler.Url);
        Assert.Equal(1, handler.Calls);
        Assert.Equal("Bearer unit-test-token", handler.Authorization);
    }

    private sealed class TestHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _status;
        public string Url { get; private set; } = "";
        public string? Authorization { get; private set; }
        public HttpMethod? Method { get; private set; }
        public int Calls { get; private set; }
        public OrderManagementPageDTO Page { get; set; } = new();
        public TestHandler(HttpStatusCode status) { _status = status; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++;
            Url = request.RequestUri!.AbsoluteUri;
            Method = request.Method;
            Authorization = request.Headers.Authorization?.ToString();
            return Task.FromResult(new HttpResponseMessage(_status)
            { Content = JsonContent.Create(Page) });
        }
    }
}
