using bmadServer.ApiService.Services.Workflows.Agents;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace bmadServer.Tests.Unit.Services.Workflows.Agents;

/// <summary>
/// Unit tests for <see cref="BmadAgentRegistry"/> - validates agent manifest loading,
/// module filtering, and agent registration capabilities.
/// </summary>
public class BmadAgentRegistryTests : IDisposable
{
    private readonly Mock<IOptions<BmadOptions>> _optionsMock;
    private readonly Mock<ILogger<BmadAgentRegistry>> _loggerMock;
    private readonly string _testManifestPath;
    private bool _disposed;

    public BmadAgentRegistryTests()
    {
        _optionsMock = new Mock<IOptions<BmadOptions>>();
        _loggerMock = new Mock<ILogger<BmadAgentRegistry>>();

        // Create a temp directory for test manifests
        _testManifestPath = Path.Combine(Path.GetTempPath(), "bmad-test", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testManifestPath);
    }

    /// <summary>
    /// Cleanup test artifacts after each test run.
    /// </summary>
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Protected implementation of Dispose pattern.
    /// </summary>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;

        if (disposing)
        {
            try
            {
                if (Directory.Exists(_testManifestPath))
                {
                    Directory.Delete(_testManifestPath, recursive: true);
                }
            }
            catch (IOException)
            {
                // Ignore cleanup errors in tests
            }
        }

        _disposed = true;
    }

    [Fact]
    public void GetAllAgents_EmptyManifest_ReturnsEmptyList()
    {
        // Arrange
        var manifestPath = CreateTestManifest("");
        _optionsMock.Setup(x => x.Value).Returns(new BmadOptions
        {
            ManifestPath = manifestPath,
            EnabledModules = []
        });

        var registry = new BmadAgentRegistry(_optionsMock.Object, _loggerMock.Object);

        // Act
        var agents = registry.GetAllAgents();

        // Assert
        Assert.Empty(agents);
    }

    [Fact]
    public void GetAllAgents_ValidManifest_ReturnsAgents()
    {
        // Arrange
        var manifestContent = """
            name,displayName,title,icon,role,identity,communicationStyle,principles,module,path
            "test-agent","Test Agent","Test Title","🧪","Test Role","Test Identity","Direct","Be helpful","core","agents/test.md"
            """;

        var manifestPath = CreateTestManifest(manifestContent);
        _optionsMock.Setup(x => x.Value).Returns(new BmadOptions
        {
            ManifestPath = manifestPath,
            BasePath = _testManifestPath,
            EnabledModules = []
        });

        var registry = new BmadAgentRegistry(_optionsMock.Object, _loggerMock.Object);

        // Act
        var agents = registry.GetAllAgents();

        // Assert
        Assert.Single(agents);
        Assert.Equal("test-agent", agents[0].AgentId);
        Assert.Equal("Test Agent", agents[0].Name);
    }

    [Fact]
    public void GetAllAgents_ModuleFiltering_OnlyReturnsEnabledModules()
    {
        // Arrange
        var manifestContent = """
            name,displayName,title,icon,role,identity,communicationStyle,principles,module,path
            "core-agent","Core Agent","Title","🧪","Role","Identity","Direct","Be helpful","core","agents/core.md"
            "bmm-agent","BMM Agent","Title","🧪","Role","Identity","Direct","Be helpful","bmm","agents/bmm.md"
            "bmgd-agent","BMGD Agent","Title","🧪","Role","Identity","Direct","Be helpful","bmgd","agents/bmgd.md"
            """;

        var manifestPath = CreateTestManifest(manifestContent);
        _optionsMock.Setup(x => x.Value).Returns(new BmadOptions
        {
            ManifestPath = manifestPath,
            BasePath = _testManifestPath,
            EnabledModules = ["core", "bmm"]
        });

        var registry = new BmadAgentRegistry(_optionsMock.Object, _loggerMock.Object);

        // Act
        var agents = registry.GetAllAgents();

        // Assert
        Assert.Equal(2, agents.Count);
        Assert.Contains(agents, a => a.AgentId == "core-agent");
        Assert.Contains(agents, a => a.AgentId == "bmm-agent");
        Assert.DoesNotContain(agents, a => a.AgentId == "bmgd-agent");
    }

    [Fact]
    public void GetAgent_ExistingAgent_ReturnsAgent()
    {
        // Arrange
        var manifestContent = """
            name,displayName,title,icon,role,identity,communicationStyle,principles,module,path
            "test-agent","Test Agent","Test Title","🧪","Test Role","Test Identity","Direct","Be helpful","core","agents/test.md"
            """;

        var manifestPath = CreateTestManifest(manifestContent);
        _optionsMock.Setup(x => x.Value).Returns(new BmadOptions
        {
            ManifestPath = manifestPath,
            BasePath = _testManifestPath,
            EnabledModules = []
        });

        var registry = new BmadAgentRegistry(_optionsMock.Object, _loggerMock.Object);

        // Act
        var agent = registry.GetAgent("test-agent");

        // Assert
        Assert.NotNull(agent);
        Assert.Equal("test-agent", agent.AgentId);
    }

    [Fact]
    public void GetAgent_NonExistingAgent_ReturnsNull()
    {
        // Arrange
        var manifestContent = """
            name,displayName,title,icon,role,identity,communicationStyle,principles,module,path
            "test-agent","Test Agent","Test Title","🧪","Test Role","Test Identity","Direct","Be helpful","core","agents/test.md"
            """;

        var manifestPath = CreateTestManifest(manifestContent);
        _optionsMock.Setup(x => x.Value).Returns(new BmadOptions
        {
            ManifestPath = manifestPath,
            BasePath = _testManifestPath,
            EnabledModules = []
        });

        var registry = new BmadAgentRegistry(_optionsMock.Object, _loggerMock.Object);

        // Act
        var agent = registry.GetAgent("non-existing-agent");

        // Assert
        Assert.Null(agent);
    }

    [Fact]
    public void GetAgent_NullOrEmptyId_ReturnsNull()
    {
        // Arrange
        _optionsMock.Setup(x => x.Value).Returns(new BmadOptions
        {
            ManifestPath = "non-existing.csv",
            EnabledModules = []
        });

        var registry = new BmadAgentRegistry(_optionsMock.Object, _loggerMock.Object);

        // Act & Assert
        Assert.Null(registry.GetAgent(null!));
        Assert.Null(registry.GetAgent(""));
        Assert.Null(registry.GetAgent("   "));
    }

    [Fact]
    public void RegisterAgent_ValidAgent_AddsToRegistry()
    {
        // Arrange
        _optionsMock.Setup(x => x.Value).Returns(new BmadOptions
        {
            ManifestPath = "non-existing.csv",
            EnabledModules = []
        });

        var registry = new BmadAgentRegistry(_optionsMock.Object, _loggerMock.Object);

        var newAgent = new AgentDefinition
        {
            AgentId = "new-agent",
            Name = "New Agent",
            SystemPrompt = "You are a new agent",
            Capabilities = ["new-capability"]
        };

        // Act
        registry.RegisterAgent(newAgent);

        // Assert
        var retrieved = registry.GetAgent("new-agent");
        Assert.NotNull(retrieved);
        Assert.Equal("new-agent", retrieved.AgentId);
    }

    private string CreateTestManifest(string content)
    {
        var manifestPath = Path.Combine(_testManifestPath, "agent-manifest.csv");
        File.WriteAllText(manifestPath, content);
        return manifestPath;
    }
}
