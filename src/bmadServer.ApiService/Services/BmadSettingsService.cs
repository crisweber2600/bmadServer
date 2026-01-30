using System.Text.Json;
using bmadServer.ApiService.DTOs;
using bmadServer.ApiService.Services.Workflows.Agents;
using bmadServer.ServiceDefaults.Services.Workflows;
using Microsoft.Extensions.Options;

namespace bmadServer.ApiService.Services;

public interface IBmadSettingsService
{
    Task<BmadSettingsResponse> GetSettingsAsync(CancellationToken cancellationToken = default);
    Task<BmadSettingsResponse> UpdateSettingsAsync(BmadSettingsRequest request, CancellationToken cancellationToken = default);
}

public class BmadSettingsService : IBmadSettingsService
{
    private readonly IOptions<BmadOptions> _options;
    private readonly IAgentRegistry _agentRegistry;
    private readonly IWorkflowRegistry _workflowRegistry;
    private readonly ILogger<BmadSettingsService> _logger;
    private readonly string _settingsFilePath;

    public BmadSettingsService(
        IOptions<BmadOptions> options,
        IAgentRegistry agentRegistry,
        IWorkflowRegistry workflowRegistry,
        IHostEnvironment hostEnvironment,
        ILogger<BmadSettingsService> logger)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _agentRegistry = agentRegistry ?? throw new ArgumentNullException(nameof(agentRegistry));
        _workflowRegistry = workflowRegistry ?? throw new ArgumentNullException(nameof(workflowRegistry));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _settingsFilePath = Path.Combine(hostEnvironment.ContentRootPath, "bmadsettings.json");
    }

    public Task<BmadSettingsResponse> GetSettingsAsync(CancellationToken cancellationToken = default)
    {
        var options = _options.Value;
        var basePath = ResolveBasePath(options.BasePath);
        var manifestInfo = LoadManifestInfo(basePath);

        return Task.FromResult(new BmadSettingsResponse
        {
            BasePath = options.BasePath,
            ManifestPath = options.ManifestPath,
            WorkflowManifestPath = options.WorkflowManifestPath,
            EnabledModules = options.EnabledModules.ToList(),
            AvailableModules = manifestInfo.Modules,
            AvailableIdes = manifestInfo.Ides,
            ManifestSourcePath = manifestInfo.ManifestSourcePath
        });
    }

    public async Task<BmadSettingsResponse> UpdateSettingsAsync(BmadSettingsRequest request, CancellationToken cancellationToken = default)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        var current = _options.Value;
        var basePath = request.BasePath?.Trim();
        if (!string.IsNullOrWhiteSpace(basePath))
        {
            if (!Directory.Exists(basePath))
            {
                throw new InvalidOperationException($"Base path not found: {basePath}");
            }
        }

        basePath ??= current.BasePath;
        var resolvedBasePath = ResolveBasePath(basePath);

        var (agentManifestPath, workflowManifestPath) = ResolveManifestPaths(
            resolvedBasePath,
            current,
            request);

        var enabledModules = request.EnabledModules?.Where(m => !string.IsNullOrWhiteSpace(m)).Distinct(StringComparer.OrdinalIgnoreCase).ToList()
            ?? current.EnabledModules.ToList();

        var updated = new BmadOptions
        {
            BasePath = basePath,
            ManifestPath = agentManifestPath,
            WorkflowManifestPath = workflowManifestPath,
            EnabledModules = enabledModules,
            TestMode = current.TestMode,
            OpenCode = current.OpenCode
        };

        await PersistSettingsAsync(updated, cancellationToken);

        var workflowOptions = new BmadWorkflowOptions
        {
            BasePath = updated.BasePath,
            ManifestPath = updated.WorkflowManifestPath,
            EnabledModules = updated.EnabledModules.ToList()
        };

        var reloadApplied = TryReloadRegistries(updated, workflowOptions);
        var manifestInfo = LoadManifestInfo(resolvedBasePath);

        return new BmadSettingsResponse
        {
            BasePath = updated.BasePath,
            ManifestPath = updated.ManifestPath,
            WorkflowManifestPath = updated.WorkflowManifestPath,
            EnabledModules = updated.EnabledModules.ToList(),
            AvailableModules = manifestInfo.Modules,
            AvailableIdes = manifestInfo.Ides,
            ManifestSourcePath = manifestInfo.ManifestSourcePath,
            ReloadApplied = reloadApplied,
            RequiresRestart = !reloadApplied,
            Message = reloadApplied
                ? "BMAD settings applied."
                : "BMAD settings saved. Restart the API service to apply changes."
        };
    }

    private bool TryReloadRegistries(BmadOptions options, BmadWorkflowOptions workflowOptions)
    {
        var reloaded = false;

        if (_agentRegistry is BmadAgentRegistry bmadAgentRegistry)
        {
            bmadAgentRegistry.Reload(options);
            reloaded = true;
        }

        if (_workflowRegistry is BmadWorkflowRegistry bmadWorkflowRegistry)
        {
            bmadWorkflowRegistry.Reload(workflowOptions);
            reloaded = true;
        }

        return reloaded;
    }

    private async Task PersistSettingsAsync(BmadOptions options, CancellationToken cancellationToken)
    {
        var payload = new BmadSettingsFile
        {
            Bmad = new BmadSettingsFileSection
            {
                BasePath = options.BasePath,
                ManifestPath = options.ManifestPath,
                WorkflowManifestPath = options.WorkflowManifestPath,
                EnabledModules = options.EnabledModules.ToList()
            }
        };

        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(_settingsFilePath, json, cancellationToken);

        _logger.LogInformation("Saved BMAD settings to {SettingsPath}", _settingsFilePath);
    }

    private static string ResolveBasePath(string? basePath)
    {
        return string.IsNullOrWhiteSpace(basePath)
            ? Directory.GetCurrentDirectory()
            : Path.GetFullPath(basePath);
    }

    private static (string AgentManifestPath, string WorkflowManifestPath) ResolveManifestPaths(
        string resolvedBasePath,
        BmadOptions current,
        BmadSettingsRequest request)
    {
        var agentManifestPath = request.ManifestPath?.Trim();
        var workflowManifestPath = request.WorkflowManifestPath?.Trim();

        if (string.IsNullOrWhiteSpace(agentManifestPath) || string.IsNullOrWhiteSpace(workflowManifestPath))
        {
            var configDir = Path.Combine(resolvedBasePath, "_config");
            var hasConfig = Directory.Exists(configDir);
            if (hasConfig)
            {
                agentManifestPath ??= Path.Combine("_config", "agent-manifest.csv");
                workflowManifestPath ??= Path.Combine("_config", "workflow-manifest.csv");
            }
        }

        agentManifestPath ??= current.ManifestPath;
        workflowManifestPath ??= current.WorkflowManifestPath;

        return (agentManifestPath, workflowManifestPath);
    }

    private static ManifestInfo LoadManifestInfo(string resolvedBasePath)
    {
        var candidates = new[]
        {
            Path.Combine(resolvedBasePath, "_config", "manifest.yaml"),
            Path.Combine(resolvedBasePath, "_config", "manifest.yml"),
            Path.Combine(resolvedBasePath, "_bmad", "_config", "manifest.yaml"),
            Path.Combine(resolvedBasePath, "_bmad", "_config", "manifest.yml")
        };

        var manifestPath = candidates.FirstOrDefault(File.Exists);
        if (manifestPath == null)
        {
            return ManifestInfo.Empty();
        }

        var modules = new List<string>();
        var ides = new List<string>();
        var currentSection = string.Empty;

        foreach (var rawLine in File.ReadAllLines(manifestPath))
        {
            var line = rawLine.Trim();
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#", StringComparison.Ordinal))
            {
                continue;
            }

            if (line.StartsWith("modules:", StringComparison.OrdinalIgnoreCase))
            {
                currentSection = "modules";
                continue;
            }

            if (line.StartsWith("ides:", StringComparison.OrdinalIgnoreCase))
            {
                currentSection = "ides";
                continue;
            }

            if (line.StartsWith("- ", StringComparison.Ordinal))
            {
                var value = line[2..].Trim();
                if (string.IsNullOrWhiteSpace(value))
                {
                    continue;
                }

                if (string.Equals(currentSection, "modules", StringComparison.OrdinalIgnoreCase))
                {
                    modules.Add(value);
                }
                else if (string.Equals(currentSection, "ides", StringComparison.OrdinalIgnoreCase))
                {
                    ides.Add(value);
                }
            }
        }

        return new ManifestInfo(modules.Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
            ides.Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
            manifestPath);
    }

    private sealed class BmadSettingsFile
    {
        public BmadSettingsFileSection Bmad { get; set; } = new();
    }

    private sealed class BmadSettingsFileSection
    {
        public string BasePath { get; set; } = string.Empty;
        public string ManifestPath { get; set; } = string.Empty;
        public string WorkflowManifestPath { get; set; } = string.Empty;
        public List<string> EnabledModules { get; set; } = [];
    }

    private sealed record ManifestInfo(List<string> Modules, List<string> Ides, string? ManifestSourcePath)
    {
        public static ManifestInfo Empty() => new([], [], null);
    }
}
