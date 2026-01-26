using System.Text.Json;

namespace bmadServer.ApiService.Data.Entities;

public class Conflict
{
    public Guid Id { get; set; }
    public Guid WorkflowInstanceId { get; set; }
    public string FieldName { get; set; } = string.Empty;
    public ConflictType Type { get; set; }
    public ConflictStatus Status { get; set; }
    public string InputsJson { get; set; } = "[]";
    public string? ResolutionJson { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? EscalatedAt { get; set; }
    public int EscalationRetries { get; set; } = 0;

    public Workflow? WorkflowInstance { get; set; }

    public List<ConflictInput> GetInputs()
    {
        return JsonSerializer.Deserialize<List<ConflictInput>>(InputsJson) ?? new List<ConflictInput>();
    }

    public void SetInputs(List<ConflictInput> inputs)
    {
        InputsJson = JsonSerializer.Serialize(inputs);
    }

    public ConflictResolution? GetResolution()
    {
        return string.IsNullOrEmpty(ResolutionJson) 
            ? null 
            : JsonSerializer.Deserialize<ConflictResolution>(ResolutionJson);
    }

    public void SetResolution(ConflictResolution? resolution)
    {
        ResolutionJson = resolution == null ? null : JsonSerializer.Serialize(resolution);
    }
}

public class ConflictInput
{
    public Guid UserId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public Guid BufferedInputId { get; set; }
}

public class ConflictResolution
{
    public Guid ResolvedBy { get; set; }
    public string ResolverDisplayName { get; set; } = string.Empty;
    public ResolutionType Type { get; set; }
    public string FinalValue { get; set; } = string.Empty;
    public DateTime ResolvedAt { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public enum ConflictType
{
    FieldValue,
    Decision,
    Checkpoint
}

public enum ConflictStatus
{
    Pending,
    Resolved,
    Escalated,
    EscalationFailed
}

public enum ResolutionType
{
    AcceptA,
    AcceptB,
    Merge,
    RejectBoth
}
