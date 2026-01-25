namespace bmadServer.ApiService.DTOs;

public class UserRolesResponse
{
    public Guid UserId { get; set; }
    public List<string> Roles { get; set; } = new();
}
