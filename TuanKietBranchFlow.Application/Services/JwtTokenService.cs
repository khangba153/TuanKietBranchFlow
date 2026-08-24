using System.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace TuanKietBranchFlow.Application.Services;

public class JwtTokenService
{
    private readonly string _key;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly int _expireMinutes;

    // Nhận cấu hình JWT qua DI
    public JwtTokenService(string key, string issuer, string audience, int expireMinutes)
    {
        _key = key;
        _issuer = issuer;
        _audience = audience;
        _expireMinutes = expireMinutes;
    }

    // Tạo JWT
    public string GenerateToken(int userId, string username, string roleCode)
    {
        // Những thông tin lưu trong token
        List<Claim> claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Name, username),
            new Claim(ClaimTypes.Role, roleCode),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        // Tạo khóa từ Jwt:Key
        SymmetricSecurityKey securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_key));
        SigningCredentials credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        // Tạo token với thông tin người dùng và thời gian hết hạn
        JwtSecurityToken token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_expireMinutes),
            signingCredentials: credentials);

        // Chuyển JWT thành chuỗi trả về client
        JwtSecurityTokenHandler tokenHandler = new JwtSecurityTokenHandler();

        return tokenHandler.WriteToken(token);
    }

}
