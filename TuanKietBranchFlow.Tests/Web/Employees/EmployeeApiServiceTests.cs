using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Mvc;
using Moq;
using TuanKietBranchFlow.Application.DTOs.Employees;
using TuanKietBranchFlow.Application.DTOs.Employees.Transfers;
using TuanKietBranchFlow.Web.Models.Employees;
using TuanKietBranchFlow.Web.Services.Auth;
using TuanKietBranchFlow.Web.Services.Common;
using TuanKietBranchFlow.Web.Services.Employees;

namespace TuanKietBranchFlow.Tests.Web.Employees;

public class EmployeeApiServiceTests
{
    [Theory]
    [InlineData(HttpStatusCode.OK)]
    [InlineData(HttpStatusCode.Conflict)]
    [InlineData(HttpStatusCode.Forbidden)]
    public async Task TransferDecision_SendsOnePostAndNeverRetries(HttpStatusCode statusCode)
    {
        using EmployeeHandler handler = new EmployeeHandler(statusCode)
        {
            Content = statusCode == HttpStatusCode.OK
                ? JsonContent.Create(new EmployeeTransferDTO { Id = 7, Status = "REJECTED" })
                : JsonContent.Create(new ProblemDetails { Detail = "Yêu cầu không còn hợp lệ." })
        };
        using HttpClient client = new HttpClient(handler) { BaseAddress = new Uri("https://unit.test/") };
        EmployeeTransferApiService service = new EmployeeTransferApiService(CreateAuthorizedService(client));

        if (statusCode == HttpStatusCode.OK)
        {
            Assert.Equal("REJECTED", (await service.DecideAsync(7, "reject", "Không tiếp nhận")).Status);
        }
        else
        {
            HttpRequestException exception = await Assert.ThrowsAsync<HttpRequestException>(() =>
                service.DecideAsync(7, "reject", "Không tiếp nhận"));
            Assert.Equal(statusCode, exception.StatusCode);
        }

        Assert.Equal(1, handler.Calls);
        Assert.Equal(HttpMethod.Post, handler.Method);
        Assert.EndsWith("/api/employee-transfers/7/reject", handler.Url);
        Assert.Equal("Bearer employee-unit-test-token", handler.Authorization);
        using JsonDocument body = JsonDocument.Parse(handler.Body!);
        Assert.Equal("Không tiếp nhận", body.RootElement.GetProperty("reason").GetString());
    }

    [Fact]
    public async Task TransferCreate_NetworkFailureDoesNotRetry()
    {
        using EmployeeHandler handler = new EmployeeHandler(HttpStatusCode.OK) { FailConnection = true };
        using HttpClient client = new HttpClient(handler) { BaseAddress = new Uri("https://unit.test/") };
        EmployeeTransferApiService service = new EmployeeTransferApiService(CreateAuthorizedService(client));

        await Assert.ThrowsAsync<HttpRequestException>(() => service.CreateAsync(new EmployeeTransferCreateDTO
        {
            EmployeeId = 12, SourceBranchId = 2, TargetBranchId = 3, Note = "Ghi chú thử"
        }));

        Assert.Equal(1, handler.Calls);
        using JsonDocument body = JsonDocument.Parse(handler.Body!);
        Assert.Equal(2, body.RootElement.GetProperty("sourceBranchId").GetInt32());
        Assert.Equal(3, body.RootElement.GetProperty("targetBranchId").GetInt32());
        Assert.Equal("Ghi chú thử", body.RootElement.GetProperty("note").GetString());
        Assert.False(body.RootElement.GetProperty("transferImmediately").GetBoolean());
    }

    [Theory]
    [InlineData(HttpStatusCode.OK)]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.Conflict)]
    [InlineData(HttpStatusCode.Forbidden)]
    public async Task Status_SendsOnePatchWithBranchBearerAndEffectiveDate(HttpStatusCode statusCode)
    {
        using EmployeeHandler handler = new EmployeeHandler(statusCode);
        using HttpClient client = new HttpClient(handler) { BaseAddress = new Uri("https://unit.test/") };
        DateOnly today = DateOnly.FromDateTime(DateTime.Today);

        EmployeeDetailApiResult result = await CreateService(client).UpdateEmployeeStatusAsync(12, 2,
            new EmployeeStatusUpdateDTO { IsActive = false, EffectiveDate = today });

        Assert.Equal(HttpMethod.Patch, handler.Method);
        Assert.EndsWith("/api/employees/12/status?branchId=2", handler.Url);
        Assert.Equal("Bearer employee-unit-test-token", handler.Authorization);
        Assert.Equal(1, handler.Calls);
        Assert.Equal((int)statusCode, result.StatusCode);
        Assert.Equal(statusCode == HttpStatusCode.OK, result.IsSuccess);
        using JsonDocument body = JsonDocument.Parse(handler.Body!);
        Assert.False(body.RootElement.GetProperty("isActive").GetBoolean());
        Assert.Equal(today.ToString("yyyy-MM-dd"), body.RootElement.GetProperty("effectiveDate").GetString());
    }

    [Fact]
    public async Task GetForEdit_SendsEmployeeBranchAndSessionBearer()
    {
        using EmployeeHandler handler = new EmployeeHandler(HttpStatusCode.OK);
        using HttpClient client = new HttpClient(handler) { BaseAddress = new Uri("https://unit.test/") };

        EmployeeDetailApiResult result = await CreateService(client).GetEmployeeForEditAsync(12, 2);

        Assert.True(result.IsSuccess);
        Assert.Equal(12, result.Employee?.Id);
        Assert.Equal(HttpMethod.Get, handler.Method);
        Assert.EndsWith("/api/employees/12?branchId=2", handler.Url);
        Assert.Equal("Bearer employee-unit-test-token", handler.Authorization);
        Assert.Equal(1, handler.Calls);
    }

    [Fact]
    public async Task Update_SendsOnePutWithEditableFieldsOnly()
    {
        using EmployeeHandler handler = new EmployeeHandler(HttpStatusCode.OK);
        using HttpClient client = new HttpClient(handler) { BaseAddress = new Uri("https://unit.test/") };
        EmployeeUpdateDTO request = CreateRequest();

        EmployeeDetailApiResult result = await CreateService(client).UpdateEmployeeAsync(12, 2, request);

        Assert.True(result.IsSuccess);
        Assert.Equal(HttpMethod.Put, handler.Method);
        Assert.EndsWith("/api/employees/12?branchId=2", handler.Url);
        Assert.Equal("Bearer employee-unit-test-token", handler.Authorization);
        using JsonDocument body = JsonDocument.Parse(handler.Body!);
        Assert.Equal(request.FullName, body.RootElement.GetProperty("fullName").GetString());
        Assert.Equal(request.BaseSalary, body.RootElement.GetProperty("baseSalary").GetDecimal());
        Assert.False(body.RootElement.TryGetProperty("isActive", out _));
        Assert.False(body.RootElement.TryGetProperty("password", out _));
        Assert.Equal(1, handler.Calls);
    }

    [Fact]
    public async Task Update_ValidationFailure_PreservesFieldErrors()
    {
        using EmployeeHandler handler = new EmployeeHandler(HttpStatusCode.BadRequest)
        {
            Content = JsonContent.Create(new ValidationProblemDetails(
                new Dictionary<string, string[]>
                {
                    { "Email", new[] { "Email không đúng định dạng." } }
                }))
        };
        using HttpClient client = new HttpClient(handler) { BaseAddress = new Uri("https://unit.test/") };

        EmployeeDetailApiResult result = await CreateService(client).UpdateEmployeeAsync(12, 2, CreateRequest());

        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.StatusCode);
        Assert.Equal("Email không đúng định dạng.", Assert.Single(result.ValidationErrors["Email"]));
        Assert.Equal(1, handler.Calls);
    }

    [Theory]
    [InlineData(HttpStatusCode.Conflict)]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.NotFound)]
    public async Task Update_ProblemFailure_KeepsStatusAndDoesNotRetry(HttpStatusCode statusCode)
    {
        using EmployeeHandler handler = new EmployeeHandler(statusCode)
        {
            Content = JsonContent.Create(new ProblemDetails { Title = "Không thể sửa", Detail = "Chi tiết lỗi thử nghiệm" })
        };
        using HttpClient client = new HttpClient(handler) { BaseAddress = new Uri("https://unit.test/") };

        EmployeeDetailApiResult result = await CreateService(client).UpdateEmployeeAsync(12, 2, CreateRequest());

        Assert.False(result.IsSuccess);
        Assert.Equal((int)statusCode, result.StatusCode);
        Assert.Equal("Chi tiết lỗi thử nghiệm", result.ErrorMessage);
        Assert.Equal(1, handler.Calls);
    }

    [Theory]
    [InlineData(HttpStatusCode.OK)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task Update_InvalidResponse_DoesNotExposeHtmlOrReturnSuccess(HttpStatusCode statusCode)
    {
        using EmployeeHandler handler = new EmployeeHandler(statusCode)
        {
            Content = new StringContent("<html>internal-error-details</html>", Encoding.UTF8, "text/html")
        };
        using HttpClient client = new HttpClient(handler) { BaseAddress = new Uri("https://unit.test/") };

        EmployeeDetailApiResult result = await CreateService(client).UpdateEmployeeAsync(12, 2, CreateRequest());

        Assert.False(result.IsSuccess);
        Assert.DoesNotContain("internal-error-details", result.ErrorMessage);
        Assert.Equal(1, handler.Calls);
    }

    [Fact]
    public async Task Update_NetworkFailure_DoesNotRetry()
    {
        using EmployeeHandler handler = new EmployeeHandler(HttpStatusCode.OK) { FailConnection = true };
        using HttpClient client = new HttpClient(handler) { BaseAddress = new Uri("https://unit.test/") };

        await Assert.ThrowsAsync<HttpRequestException>(() =>
            CreateService(client).UpdateEmployeeAsync(12, 2, CreateRequest()));

        Assert.Equal(1, handler.Calls);
    }

    private static EmployeeUpdateDTO CreateRequest()
    {
        return new EmployeeUpdateDTO
        {
            FullName = "Nguyễn Thị Thủy",
            EmployeeCode = "NV-TEST",
            Email = "unit@example.test",
            Phone = "0900000000",
            HireDate = new DateOnly(2026, 1, 1),
            BaseSalary = 7500000,
            Position = "Nhân viên order",
            Address = "Địa chỉ thử nghiệm"
        };
    }

    private static EmployeeApiService CreateService(HttpClient client)
    {
        return new EmployeeApiService(CreateAuthorizedService(client));
    }

    private static AuthorizedApiService CreateAuthorizedService(HttpClient client)
    {
        Guid sessionId = Guid.NewGuid();
        Mock<AuthenticationStateProvider> authenticationStateProvider = new Mock<AuthenticationStateProvider>();
        ClaimsPrincipal user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(WebCookieAuthenticationEvents.WebSessionIdClaim, sessionId.ToString())
        }, "TestCookie"));
        authenticationStateProvider.Setup(provider => provider.GetAuthenticationStateAsync())
            .ReturnsAsync(new AuthenticationState(user));
        Mock<IWebTokenService> webTokenService = new Mock<IWebTokenService>();
        webTokenService.Setup(service => service.GetAccessTokenAsync(sessionId))
            .ReturnsAsync("employee-unit-test-token");
        Mock<IHttpClientFactory> httpClientFactory = new Mock<IHttpClientFactory>();
        httpClientFactory.Setup(factory => factory.CreateClient("BranchFlowApi")).Returns(client);

        return new AuthorizedApiService(
            httpClientFactory.Object,
            authenticationStateProvider.Object,
            webTokenService.Object);
    }

    private sealed class EmployeeHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _statusCode;
        public HttpContent? Content { get; set; }
        public bool FailConnection { get; set; }
        public int Calls { get; private set; }
        public string? Url { get; private set; }
        public string? Authorization { get; private set; }
        public HttpMethod? Method { get; private set; }
        public string? Body { get; private set; }

        public EmployeeHandler(HttpStatusCode statusCode)
        {
            _statusCode = statusCode;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Calls++;
            Url = request.RequestUri?.AbsoluteUri;
            Method = request.Method;
            Authorization = request.Headers.Authorization?.ToString();

            if (request.Content is not null)
            {
                Body = await request.Content.ReadAsStringAsync(cancellationToken);
            }

            if (FailConnection)
            {
                throw new HttpRequestException("Connection interrupted");
            }

            return new HttpResponseMessage(_statusCode)
            {
                Content = Content ?? JsonContent.Create(new EmployeeDetailDTO
                {
                    Id = 12,
                    FullName = "Nguyễn Thị Thủy",
                    EmployeeCode = "NV-TEST",
                    HireDate = new DateOnly(2026, 1, 1)
                })
            };
        }
    }
}
