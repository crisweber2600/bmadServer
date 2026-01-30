using Microsoft.Extensions.Options;

namespace bmadServer.ApiService.Services.Workflows.Agents;

/// <summary>
/// Factory for creating agent handlers based on the configured test mode
/// </summary>
public interface IAgentHandlerFactory
{
    /// <summary>
    /// Create an agent handler for the specified agent definition
    /// </summary>
    /// <param name="agentDefinition">The agent definition to create a handler for</param>
    /// <param name="modelOverride">Optional model override for this handler</param>
    /// <returns>An agent handler appropriate for the configured test mode</returns>
    IAgentHandler CreateHandler(AgentDefinition agentDefinition, string? modelOverride = null);
}

/// <summary>
/// Factory implementation that creates handlers based on BmadOptions.TestMode
/// </summary>
public class AgentHandlerFactory : IAgentHandlerFactory
{
    private readonly BmadOptions _bmadOptions;
    private readonly IOptions<OpenCodeOptions> _openCodeOptions;
    private readonly IOptions<CopilotOptions> _copilotOptions;
    private readonly IOptions<BmadOptions> _bmadOptionsWrapper;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<AgentHandlerFactory> _logger;

    public AgentHandlerFactory(
        IOptions<BmadOptions> bmadOptions,
        IOptions<OpenCodeOptions> openCodeOptions,
        IOptions<CopilotOptions> copilotOptions,
        ILoggerFactory loggerFactory)
    {
        _bmadOptions = bmadOptions?.Value ?? throw new ArgumentNullException(nameof(bmadOptions));
        _bmadOptionsWrapper = bmadOptions;
        _openCodeOptions = openCodeOptions ?? throw new ArgumentNullException(nameof(openCodeOptions));
        _copilotOptions = copilotOptions ?? throw new ArgumentNullException(nameof(copilotOptions));
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        _logger = loggerFactory.CreateLogger<AgentHandlerFactory>();
    }

    /// <inheritdoc />
    public IAgentHandler CreateHandler(AgentDefinition agentDefinition, string? modelOverride = null)
    {
        if (agentDefinition == null)
        {
            throw new ArgumentNullException(nameof(agentDefinition));
        }

        _logger.LogDebug(
            "Creating handler for agent {AgentId} in {TestMode} mode",
            agentDefinition.AgentId, _bmadOptions.TestMode);

        return _bmadOptions.TestMode switch
        {
            AgentTestMode.Mock => CreateMockHandler(agentDefinition),
            AgentTestMode.Live => CreateLiveHandler(agentDefinition, modelOverride),
            AgentTestMode.Replay => CreateReplayHandler(agentDefinition, modelOverride),
            _ => throw new ArgumentOutOfRangeException(nameof(_bmadOptions.TestMode))
        };
    }

    private IAgentHandler CreateMockHandler(AgentDefinition agentDefinition)
    {
        _logger.LogDebug("Creating MockAgentHandler for {AgentId}", agentDefinition.AgentId);
        return new MockAgentHandler(shouldSucceed: true);
    }

    private IAgentHandler CreateLiveHandler(AgentDefinition agentDefinition, string? modelOverride)
    {
        _logger.LogDebug("Creating CopilotAgentHandler for {AgentId}", agentDefinition.AgentId);
        return new CopilotAgentHandler(
            agentDefinition,
            _copilotOptions,
            _loggerFactory.CreateLogger<CopilotAgentHandler>(),
            modelOverride);
    }

    private IAgentHandler CreateReplayHandler(AgentDefinition agentDefinition, string? modelOverride)
    {
        _logger.LogDebug("Creating ReplayAgentHandler for {AgentId}", agentDefinition.AgentId);
        return new ReplayAgentHandler(
            agentDefinition,
            _openCodeOptions,
            _bmadOptionsWrapper,
            _loggerFactory.CreateLogger<ReplayAgentHandler>(),
            _loggerFactory,
            modelOverride);
    }
}
