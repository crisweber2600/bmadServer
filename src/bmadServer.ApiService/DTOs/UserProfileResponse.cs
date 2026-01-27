namespace bmadServer.ApiService.DTOs;

/// <summary>
/// User profile response for attribution display (Story 7.3)
/// </summary>
public class UserProfileResponse
{
    public Guid UserId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public DateTime JoinedAt { get; set; }
    public string Role { get; set; } = string.Empty;
}
