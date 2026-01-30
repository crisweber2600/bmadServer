using bmadServer.ApiService.Services.Workflows.Agents;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace bmadServer.Tests.Unit.Services.Workflows.Agents;

public class AgentHandlerFactoryTests
{
    private readonly Mock<IOptions<BmadOptions>> _bmadOptionsMock;
    private readonly Mock<IOptions<OpenCodeOptions>> _openCodeOptionsMock;
    private readonly Mock<IOptions<CopilotOptions>> _copilotOptionsMock;
    private readonly Mock<ILoggerFactory> _loggerFactoryMock;
    private readonly Mock<ILogger<AgentHandlerFactory>> _loggerMock;

    public AgentHandlerFactoryTests()
    {
        _bmadOptionsMock = new Mock<IOptions<BmadOptions>>();
        _openCodeOptionsMock = new Mock<IOptions<OpenCodeOptions>>();
        _copilotOptionsMock = new Mock<IOptions<CopilotOptions>>();
        _loggerFactoryMock = new Mock<ILoggerFactory>();
        _loggerMock = new Mock<ILogger<AgentHandlerFactory>>();

        _openCodeOptionsMock.Setup(x => x.Value).Returns(new OpenCodeOptions());
        _copilotOptionsMock.Setup(x => x.Value).Returns(new CopilotOptions());
        _loggerFactoryMock.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(_loggerMock.Object);
    }

    [Fact]
    public void CreateHandler_MockMode_ReturnsMockAgentHandler()
    {
        // Arrange
        _bmadOptionsMock.Setup(x => x.Value).Returns(new BmadOptions
        {
            TestMode = AgentTestMode.Mock
        });

        var factory = new AgentHandlerFactory(
            _bmadOptionsMock.Object,
            _openCodeOptionsMock.Object,
            _copilotOptionsMock.Object,
            _loggerFactoryMock.Object);

        var agentDef = CreateTestAgentDefinition();

        // Act
        var handler = factory.CreateHandler(agentDef);

        // Assert
        Assert.IsType<MockAgentHandler>(handler);
    }

    [Fact]
    public void CreateHandler_LiveMode_ReturnsCopilotAgentHandler()
    {
        // Arrange
        _bmadOptionsMock.Setup(x => x.Value).Returns(new BmadOptions
        {
            TestMode = AgentTestMode.Live
        });

        var factory = new AgentHandlerFactory(
            _bmadOptionsMock.Object,
            _openCodeOptionsMock.Object,
            _copilotOptionsMock.Object,
            _loggerFactoryMock.Object);

        var agentDef = CreateTestAgentDefinition();

        // Act
        var handler = factory.CreateHandler(agentDef);

        // Assert
        Assert.IsType<CopilotAgentHandler>(handler);
    }

    [Fact]
    public void CreateHandler_ReplayMode_ReturnsReplayAgentHandler()
    {
        // Arrange
        _bmadOptionsMock.Setup(x => x.Value).Returns(new BmadOptions
        {
            TestMode = AgentTestMode.Replay,
            BasePath = Path.GetTempPath()
        });

        var factory = new AgentHandlerFactory(
            _bmadOptionsMock.Object,
            _openCodeOptionsMock.Object,
            _copilotOptionsMock.Object,
            _loggerFactoryMock.Object);

        var agentDef = CreateTestAgentDefinition();

        // Act
        var handler = factory.CreateHandler(agentDef);

        // Assert
        Assert.IsType<ReplayAgentHandler>(handler);
    }

    [Fact]
    public void CreateHandler_NullAgentDefinition_ThrowsArgumentNullException()
    {
        // Arrange
        _bmadOptionsMock.Setup(x => x.Value).Returns(new BmadOptions());

        var factory = new AgentHandlerFactory(
            _bmadOptionsMock.Object,
            _openCodeOptionsMock.Object,
            _copilotOptionsMock.Object,
            _loggerFactoryMock.Object);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => factory.CreateHandler(null!));
    }

    private static AgentDefinition CreateTestAgentDefinition()
    {
        return new AgentDefinition
        {
            AgentId = "test-agent",
            Name = "Test Agent",
            Description = "A test agent for unit testing",
            SystemPrompt = "You are a test agent",
            Capabilities = ["test-capability"],
            ModelPreference = "gpt-4",
            Temperature = 0.7m
        };
    }
}
