using bmadServer.ApiService.Data.Entities;

namespace bmadServer.ApiService.Services;

public interface IJwtTokenService
{
    string GenerateAccessToken(User user);
    
    string GenerateAccessToken(User user, IEnumerable<string> roles);
    
    string GenerateAccessToken(User user, IEnumerable<Role> roles);
}
