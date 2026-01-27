namespace bmadServer.ApiService.DTOs;

public class ConflictDto
{
    public Guid Id { get; set; }
    public Guid WorkflowInstanceId { get; set; }
    public string FieldName { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public List<ConflictInputDto> Inputs { get; set; } = new();
    public ConflictResolutionDto? Resolution { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? EscalatedAt { get; set; }
}

public class ConflictInputDto
{
    public Guid UserId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}

public class ConflictResolutionDto
{
    public Guid ResolvedBy { get; set; }
    public string ResolverDisplayName { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string FinalValue { get; set; } = string.Empty;
    public DateTime ResolvedAt { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public class ResolveConflictRequest
{
    public string ResolutionType { get; set; } = string.Empty;
    public string? FinalValue { get; set; }
    public string Reason { get; set; } = string.Empty;
}
