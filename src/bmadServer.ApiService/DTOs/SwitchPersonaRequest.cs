namespace bmadServer.ApiService.DTOs;

public record SwitchPersonaRequest
{
    public required string PersonaType { get; init; } // "Business", "Technical", or "Hybrid"
}
