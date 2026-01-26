namespace bmadServer.ApiService.Data.Entities;

public class TranslationMapping
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string TechnicalTerm { get; set; }
    public required string BusinessTerm { get; set; }
    public string? Context { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
