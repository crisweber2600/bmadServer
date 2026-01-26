using bmadServer.ApiService.Data.Entities;

namespace bmadServer.ApiService.DTOs;

public record SwitchPersonaResponse
{
    public required Guid SessionId { get; init; }
    public required PersonaType NewPersona { get; init; }
    public required PersonaType PreviousPersona { get; init; }
    public int SwitchCount { get; init; }
    public string? SuggestionMessage { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}
