namespace bmadServer.ApiService.DTOs;

public class BmadSettingsRequest
{
    public string? BasePath { get; set; }
    public string? ManifestPath { get; set; }
    public string? WorkflowManifestPath { get; set; }
    public List<string>? EnabledModules { get; set; }
}

public class BmadSettingsResponse
{
    public string BasePath { get; set; } = string.Empty;
    public string ManifestPath { get; set; } = string.Empty;
    public string WorkflowManifestPath { get; set; } = string.Empty;
    public List<string> EnabledModules { get; set; } = [];
    public List<string> AvailableModules { get; set; } = [];
    public List<string> AvailableIdes { get; set; } = [];
    public string? ManifestSourcePath { get; set; }
    public bool ReloadApplied { get; set; }
    public bool RequiresRestart { get; set; }
    public string? Message { get; set; }
}