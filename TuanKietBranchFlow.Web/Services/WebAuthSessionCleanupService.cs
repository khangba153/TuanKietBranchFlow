namespace TuanKietBranchFlow.Web.Services;

// Dọn các phiên Web hết hạn trong lúc ứng dụng đang chạy
public class WebAuthSessionCleanupService : BackgroundService
{
    private readonly IWebAuthSessionStore _sessionStore;
    private readonly ILogger<WebAuthSessionCleanupService> _logger;

    // Nhận kho RAM dùng chung và dịch vụ ghi log
    public WebAuthSessionCleanupService(
        IWebAuthSessionStore sessionStore,
        ILogger<WebAuthSessionCleanupService> logger)
    {
        _sessionStore = sessionStore;
        _logger = logger;
    }

    // Chờ từng nhịp một phút rồi dọn phiên hết hạn
    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        using PeriodicTimer timer =
            new PeriodicTimer(TimeSpan.FromMinutes(1));

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                int removedCount = _sessionStore.RemoveExpiredSessions();

                // Chỉ ghi số lượng, không ghi token hoặc thông tin tài khoản
                if (removedCount > 0)
                {
                    _logger.LogInformation(
                        "Đã dọn {RemovedCount} phiên Web hết hạn khỏi RAM.",
                        removedCount);
                }
            }
        }
        catch (OperationCanceledException)
            when (stoppingToken.IsCancellationRequested)
        {
            // Web đang dừng; kết thúc công việc nền bình thường
        }
    }
}