namespace bmadServer.ApiService.Models.Events;

public class DecisionMadeEvent
{
    public string Decision { get; set; } = string.Empty;
    public List<string>? Alternatives { get; set; }
    public double? Confidence { get; set; }
}
