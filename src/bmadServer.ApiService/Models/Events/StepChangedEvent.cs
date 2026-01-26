namespace bmadServer.ApiService.Models.Events;

public class StepChangedEvent
{
    public string StepId { get; set; } = string.Empty;
    public string StepName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}
