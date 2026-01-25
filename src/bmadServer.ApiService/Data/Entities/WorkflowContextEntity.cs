using bmadServer.ApiService.WorkflowContext;
using System.Text.Json;

namespace bmadServer.ApiService.Data.Entities;

/// <summary>
/// Database entity for storing workflow context.
/// Stores the full SharedContext as JSONB for flexibility.
/// </summary>
public class WorkflowContextEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid WorkflowInstanceId { get; set; }
    public string ContextData { get; set; } = string.Empty;
    public int Version { get; set; } = 1;
    public DateTime LastModifiedAt { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Converts the entity to a SharedContext domain object.
    /// </summary>
    public SharedContext ToSharedContext()
    {
        var context = JsonSerializer.Deserialize<SharedContext>(ContextData);
        if (context == null)
        {
            throw new InvalidOperationException("Failed to deserialize workflow context");
        }
        return context;
    }

    /// <summary>
    /// Creates a new entity from a SharedContext domain object.
    /// </summary>
    public static WorkflowContextEntity FromSharedContext(SharedContext context)
    {
        return new WorkflowContextEntity
        {
            WorkflowInstanceId = context.WorkflowInstanceId,
            ContextData = JsonSerializer.Serialize(context),
            Version = context.Version,
            LastModifiedAt = context.LastModifiedAt
        };
    }

    /// <summary>
    /// Updates the entity from a SharedContext domain object.
    /// </summary>
    public void UpdateFromSharedContext(SharedContext context)
    {
        ContextData = JsonSerializer.Serialize(context);
        Version = context.Version;
        LastModifiedAt = context.LastModifiedAt;
    }
}
