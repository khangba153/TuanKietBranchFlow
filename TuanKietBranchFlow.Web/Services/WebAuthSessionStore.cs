using System.Collections.Concurrent;
using TuanKietBranchFlow.Web.Models;

namespace TuanKietBranchFlow.Web.Services;

// Giữ dữ liệu riêng của từng phiên trong RAM Web
public class WebAuthSessionStore : IWebAuthSessionStore
{
    // Khóa là mã phiên Web, giá trị là dữ liệu của phiên
    private readonly ConcurrentDictionary<Guid, WebAuthSession> _sessions =
        new ConcurrentDictionary<Guid, WebAuthSession>();
 
    // Giới hạn số phiên được giữ trong kho RAM
    private readonly int _maxSessions;

    // Đồng bộ thao tác kiểm tra giới hạn và thêm phiên
    private readonly object _addLock = new object();

    // Đọc giới hạn từ cấu hình, mặc định 1.000 phiên cho bản demo
    public WebAuthSessionStore(IConfiguration configuration)
    {
        _maxSessions = configuration.GetValue<int>(
            "WebAuth:MaxSessions", 1000);

        if (_maxSessions <= 0)
        {
            throw new InvalidOperationException(
                "WebAuth:MaxSessions phải lớn hơn 0.");
        }
    }

    // Chỉ thêm phiên hợp lệ khi kho còn chỗ và mã phiên chưa tồn tại
    public bool TryAdd(WebAuthSession session)
    {
        if (session.Id == Guid.Empty ||
            session.RefreshTokenExpiresAt <= DateTime.UtcNow)
        {
            return false;
        }

        // Dọn phiên hết hạn để giải phóng chỗ trước khi thêm
        RemoveExpiredSessions();

        lock (_addLock)
        {
            if (_sessions.Count >= _maxSessions)
            {
                return false;
            }

            return _sessions.TryAdd(session.Id, session);
        }
    }

    // Tìm phiên còn hạn; bỏ phiên hết hạn khỏi RAM
    public WebAuthSession? GetById(Guid sessionId)
    {
        if (!_sessions.TryGetValue(sessionId, out WebAuthSession? session))
        {
            return null;
        }

        // Refresh hết hạn cũng là lúc phiên không thể tiếp tục
        if (session.RefreshTokenExpiresAt <= DateTime.UtcNow)
        {
            Remove(sessionId);
            return null;
        }

        return session;
    }

    // Bỏ dữ liệu phiên khỏi RAM, không gọi API logout
    public void Remove(Guid sessionId)
    {
        _sessions.TryRemove(sessionId, out _);
    }

    // Quét kho và bỏ những phiên có refresh token hết hạn
    public int RemoveExpiredSessions()
    {
        DateTime now = DateTime.UtcNow;
        int removedCount = 0;

        foreach (KeyValuePair<Guid, WebAuthSession> entry in _sessions)
        {
            if (entry.Value.RefreshTokenExpiresAt <= now)
            {
                if (_sessions.TryRemove(entry.Key, out _))
                {
                    removedCount++;
                }
            }
        }

        return removedCount;
    }

    // Sao chép mã các phiên của tài khoản để phục vụ dọn RAM khi logout.
    public List<Guid> GetSessionIdsByUserId(int userId)
    {
        List<Guid> sessionIds = new List<Guid>();

        if (userId <= 0)
        {
            return sessionIds;
        }

        foreach (KeyValuePair<Guid, WebAuthSession> entry in _sessions)
        {
            if (entry.Value.UserId == userId)
            {
                sessionIds.Add(entry.Key);
            }
        }

        return sessionIds;
    }
}