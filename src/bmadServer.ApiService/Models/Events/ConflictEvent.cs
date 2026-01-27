namespace bmadServer.ApiService.Models.Events;

public class ConflictEvent
{
    public Guid ConflictId { get; set; }
    public string FieldName { get; set; } = string.Empty;
    public List<string> ConflictingValues { get; set; } = new();
}
