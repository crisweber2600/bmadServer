using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using bmadServer.ApiService.Configuration;
using bmadServer.ApiService.Data.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace bmadServer.ApiService.Services;

/// <summary>
/// Service for generating JWT tokens
/// </summary>
public class JwtTokenService : IJwtTokenService
{
    private readonly JwtSettings _settings;

    public JwtTokenService(IOptions<JwtSettings> settings)
    {
        _settings = settings.Value;
        _settings.Validate();
    }

    public string GenerateAccessToken(User user)
    {
        return GenerateAccessToken(user, Enumerable.Empty<string>());
    }

    public string GenerateAccessToken(User user, IEnumerable<Role> roles)
    {
        return GenerateAccessToken(user, roles.Select(r => r.ToString()));
    }
    
    public string GenerateAccessToken(User user, IEnumerable<string> roles)
    {
        var securityKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_settings.SecretKey));
        var credentials = new SigningCredentials(
            securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat,
                DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
                ClaimValueTypes.Integer64)
        };
        
        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_settings.AccessTokenExpirationMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
