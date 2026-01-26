namespace bmadServer.ApiService.DTOs;

public class AddParticipantRequest
{
    public required Guid UserId { get; set; }
    public required string Role { get; set; }
}
