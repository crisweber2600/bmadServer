using System.Text.Json;

namespace bmadServer.ApiService.DTOs.Checkpoints;

public record QueueInputRequest(
    string InputType,
    JsonDocument Content
);

public record CheckpointResponse(
    Guid Id,
    Guid WorkflowId,
    string StepId,
    string CheckpointType,
    long Version,
    DateTime CreatedAt,
    Guid TriggeredBy,
    JsonDocument? Metadata
);

public record QueuedInputResponse(
    Guid Id,
    Guid WorkflowId,
    Guid UserId,
    string InputType,
    JsonDocument Content,
    DateTime QueuedAt,
    DateTime? ProcessedAt,
    string Status,
    string? RejectionReason
);

public record PagedResult<T>(
    List<T> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages
);

public record InputProcessingResult(
    int ProcessedCount,
    int RejectedCount,
    List<string> Errors
);
