using bmadServer.ApiService.Data;
using bmadServer.ApiService.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace bmadServer.ApiService.BackgroundServices;

public class ConflictEscalationJob : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ConflictEscalationJob> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(5);

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
                await CheckExpiredConflictsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ConflictEscalationJob");
            }

            await Task.Delay(_checkInterval, stoppingToken);
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
                "Conflict {ConflictId} escalated for workflow {WorkflowId}",
                conflict.Id, conflict.WorkflowInstanceId);
        }

        if (expiredConflicts.Any())
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
