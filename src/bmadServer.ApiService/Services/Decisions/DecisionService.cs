using bmadServer.ApiService.Data;
using bmadServer.ApiService.Data.Entities;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace bmadServer.ApiService.Services.Decisions;

/// <summary>
/// Service implementation for managing workflow decisions
/// </summary>
public class DecisionService : IDecisionService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DecisionService> _logger;

    public DecisionService(ApplicationDbContext context, ILogger<DecisionService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<Decision> CreateDecisionAsync(Decision decision, CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate that the workflow instance exists
            var workflowExists = await _context.WorkflowInstances
                .AnyAsync(w => w.Id == decision.WorkflowInstanceId, cancellationToken);

            if (!workflowExists)
            {
                throw new InvalidOperationException($"Workflow instance {decision.WorkflowInstanceId} does not exist");
            }

            _context.Decisions.Add(decision);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Decision {DecisionId} created for workflow {WorkflowInstanceId}, step {StepId}, type {DecisionType}",
                decision.Id,
                decision.WorkflowInstanceId,
                decision.StepId,
                decision.DecisionType
            );

            return decision;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Failed to create decision for workflow {WorkflowInstanceId}", 
                decision.WorkflowInstanceId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<List<Decision>> GetDecisionsByWorkflowInstanceAsync(
        Guid workflowInstanceId, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var decisions = await _context.Decisions
                .Where(d => d.WorkflowInstanceId == workflowInstanceId)
                .OrderBy(d => d.DecidedAt)
                .ToListAsync(cancellationToken);

            _logger.LogInformation(
                "Retrieved {Count} decisions for workflow {WorkflowInstanceId}",
                decisions.Count,
                workflowInstanceId
            );

            return decisions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Failed to retrieve decisions for workflow {WorkflowInstanceId}", 
                workflowInstanceId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<Decision?> GetDecisionByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var decision = await _context.Decisions
                .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

            if (decision != null)
            {
                _logger.LogInformation("Retrieved decision {DecisionId}", id);
            }
            else
            {
                _logger.LogWarning("Decision {DecisionId} not found", id);
            }

            return decision;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve decision {DecisionId}", id);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<Decision> UpdateDecisionAsync(
        Guid id,
        Guid userId,
        JsonDocument value,
        string? question,
        JsonDocument? options,
        string? reasoning,
        JsonDocument? context,
        string? changeReason,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var decision = await _context.Decisions
                .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

            if (decision == null)
            {
                throw new InvalidOperationException($"Decision {id} not found");
            }

            // Create version record with current values
            var version = new DecisionVersion
            {
                DecisionId = decision.Id,
                VersionNumber = decision.CurrentVersion,
                Value = decision.Value,
                Question = decision.Question,
                Options = decision.Options,
                Reasoning = decision.Reasoning,
                Context = decision.Context,
                ModifiedBy = userId,
                ModifiedAt = DateTime.UtcNow,
                ChangeReason = changeReason
            };

            _context.DecisionVersions.Add(version);

            // Update decision with new values
            decision.Value = value;
            decision.Question = question;
            decision.Options = options;
            decision.Reasoning = reasoning;
            decision.Context = context;
            decision.CurrentVersion++;
            decision.UpdatedAt = DateTime.UtcNow;
            decision.UpdatedBy = userId;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Decision {DecisionId} updated to version {Version} by user {UserId}",
                id,
                decision.CurrentVersion,
                userId
            );

            return decision;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update decision {DecisionId}", id);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<List<DecisionVersion>> GetDecisionHistoryAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var versions = await _context.DecisionVersions
                .Where(v => v.DecisionId == id)
                .OrderBy(v => v.VersionNumber)
                .ToListAsync(cancellationToken);

            _logger.LogInformation(
                "Retrieved {Count} versions for decision {DecisionId}",
                versions.Count,
                id
            );

            return versions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve version history for decision {DecisionId}", id);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<DecisionVersion?> GetDecisionVersionAsync(Guid id, int versionNumber, CancellationToken cancellationToken = default)
    {
        try
        {
            var version = await _context.DecisionVersions
                .FirstOrDefaultAsync(v => v.DecisionId == id && v.VersionNumber == versionNumber, cancellationToken);

            if (version != null)
            {
                _logger.LogInformation(
                    "Retrieved version {Version} for decision {DecisionId}",
                    versionNumber,
                    id
                );
            }
            else
            {
                _logger.LogWarning(
                    "Version {Version} not found for decision {DecisionId}",
                    versionNumber,
                    id
                );
            }

            return version;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Failed to retrieve version {Version} for decision {DecisionId}",
                versionNumber,
                id);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<Decision> RevertDecisionAsync(
        Guid id,
        int versionNumber,
        Guid userId,
        string? reason,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var decision = await _context.Decisions
                .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

            if (decision == null)
            {
                throw new InvalidOperationException($"Decision {id} not found");
            }

            var targetVersion = await _context.DecisionVersions
                .FirstOrDefaultAsync(v => v.DecisionId == id && v.VersionNumber == versionNumber, cancellationToken);

            if (targetVersion == null)
            {
                throw new InvalidOperationException($"Version {versionNumber} not found for decision {id}");
            }

            // Create version record with current values before reverting
            var currentVersion = new DecisionVersion
            {
                DecisionId = decision.Id,
                VersionNumber = decision.CurrentVersion,
                Value = decision.Value,
                Question = decision.Question,
                Options = decision.Options,
                Reasoning = decision.Reasoning,
                Context = decision.Context,
                ModifiedBy = userId,
                ModifiedAt = DateTime.UtcNow,
                ChangeReason = $"Revert to version {versionNumber}: {reason}"
            };

            _context.DecisionVersions.Add(currentVersion);

            // Revert to target version values
            decision.Value = targetVersion.Value;
            decision.Question = targetVersion.Question;
            decision.Options = targetVersion.Options;
            decision.Reasoning = targetVersion.Reasoning;
            decision.Context = targetVersion.Context;
            decision.CurrentVersion++;
            decision.UpdatedAt = DateTime.UtcNow;
            decision.UpdatedBy = userId;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Decision {DecisionId} reverted to version {TargetVersion}, now at version {CurrentVersion}",
                id,
                versionNumber,
                decision.CurrentVersion
            );

            return decision;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to revert decision {DecisionId} to version {Version}",
                id,
                versionNumber);
            throw;
        }
    }
}
