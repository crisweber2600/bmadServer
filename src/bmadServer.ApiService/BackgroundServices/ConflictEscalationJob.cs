using bmadServer.ApiService.Data;
using bmadServer.ApiService.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace bmadServer.ApiService.BackgroundServices;

public class ConflictEscalationJob : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ConflictEscalationJob> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(1);
    private readonly int _maxRetries = 3;

    public ConflictEscalationJob(
        IServiceProvider serviceProvider,
        ILogger<ConflictEscalationJob> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Conflict Escalation Job started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await EscalatePendingConflictsAsync(stoppingToken);
                await CheckExpiredConflictsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ConflictEscalationJob");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }
    }

    /// <summary>
    /// Escalate pending conflicts with retry logic.
    /// </summary>
    private async Task EscalatePendingConflictsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var pendingConflicts = await dbContext.Conflicts
            .Where(c => c.Status == ConflictStatus.Pending && c.EscalationRetries < _maxRetries)
            .ToListAsync(cancellationToken);

        foreach (var conflict in pendingConflicts)
        {
            using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                conflict.Status = ConflictStatus.Escalated;
                conflict.EscalatedAt = DateTime.UtcNow;
                conflict.EscalationRetries++;

                await dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                _logger.LogWarning(
                    "Conflict {ConflictId} escalated successfully for workflow {WorkflowId}",
                    conflict.Id, conflict.WorkflowInstanceId);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                conflict.EscalationRetries++;

                _logger.LogError(ex,
                    "Failed to escalate conflict {ConflictId}. Attempt {Attempt}/{MaxRetries}",
                    conflict.Id, conflict.EscalationRetries, _maxRetries);

                if (conflict.EscalationRetries >= _maxRetries)
                {
                    conflict.Status = ConflictStatus.EscalationFailed;
                    _logger.LogError(
                        "Conflict {ConflictId} escalation failed after {MaxRetries} attempts",
                        conflict.Id, _maxRetries);

                    await dbContext.SaveChangesAsync(cancellationToken);
                }
            }
        }
    }

    private async Task CheckExpiredConflictsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var expiredConflicts = await dbContext.Conflicts
            .Where(c => c.Status == ConflictStatus.Pending 
                     && c.ExpiresAt <= DateTime.UtcNow)
            .ToListAsync(cancellationToken);

        foreach (var conflict in expiredConflicts)
        {
            conflict.Status = ConflictStatus.Escalated;
            conflict.EscalatedAt = DateTime.UtcNow;

            _logger.LogWarning(
                "Conflict {ConflictId} auto-escalated due to expiration for workflow {WorkflowId}",
                conflict.Id, conflict.WorkflowInstanceId);
        }

        if (expiredConflicts.Any())
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
