namespace bmadServer.ApiService.Configuration;

public class SparkCompatRolloutOptions
{
    public const string SectionName = "SparkCompatRollout";

    public bool EnableCompatV1 { get; set; } = true;
    public bool EnableAuth { get; set; } = true;
    public bool EnablePresence { get; set; } = true;
    public bool EnableChats { get; set; } = true;
    public bool EnablePullRequests { get; set; } = true;
    public bool EnableCollaborationEvents { get; set; } = true;
    public bool EnableDecisionCenter { get; set; } = true;
}
