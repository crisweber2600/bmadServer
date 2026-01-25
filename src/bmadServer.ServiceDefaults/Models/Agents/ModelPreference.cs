namespace bmadServer.ServiceDefaults.Models.Agents;

public class ModelPreference
{
    public required string PreferredModel { get; init; }
    public required string FallbackModel { get; init; }
    public int MaxTokens { get; init; }
    public double Temperature { get; init; }
}
