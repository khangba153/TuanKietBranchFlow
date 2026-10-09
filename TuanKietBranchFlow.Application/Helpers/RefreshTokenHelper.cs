using System.Security.Cryptography;
using System.Text;

namespace TuanKietBranchFlow.Application.Helpers;

public static class RefreshTokenHelper
{
    // Tạo refresh token ngẫu nhiên
    public static string GenerateToken()
    {
        byte[] randomBytes = RandomNumberGenerator.GetBytes(32);

        return Convert.ToBase64String(randomBytes);
    }

    // Tạo hash của refresh token để lưu và đối chiếu trong database
    public static string HashToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new ArgumentException(
                "Refresh token không được để trống.",
                nameof(token));
        }

        byte[] tokenBytes = Encoding.UTF8.GetBytes(token);

        byte[] hashBytes = SHA256.HashData(tokenBytes);

        return Convert.ToHexString(hashBytes);
    }
}
