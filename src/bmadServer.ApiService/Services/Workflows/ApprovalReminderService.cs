using bmadServer.ApiService.Hubs;
using bmadServer.ApiService.Models.Workflows;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;

namespace bmadServer.ApiService.Services.Workflows;

public class ApprovalTimeoutOptions
{
    public TimeSpan ReminderThreshold { get; set; } = TimeSpan.FromHours(24);
    public TimeSpan TimeoutThreshold { get; set; } = TimeSpan.FromHours(72);
    public TimeSpan CheckInterval { get; set; } = TimeSpan.FromHours(1);
}

public class ApprovalReminderService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHubContext<ChatHub> _hubContext;
    private readonly ILogger<ApprovalReminderService> _logger;
    private readonly ApprovalTimeoutOptions _options;

    public ApprovalReminderService(
        IServiceScopeFactory scopeFactory,
        IHubContext<ChatHub> hubContext,
        IOptions<ApprovalTimeoutOptions> options,
        ILogger<ApprovalReminderService> logger)
    {
        _scopeFactory = scopeFactory;
        _hubContext = hubContext;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "ApprovalReminderService started. Reminder: {Reminder}h, Timeout: {Timeout}h, Interval: {Interval}h",
            _options.ReminderThreshold.TotalHours,
            _options.TimeoutThreshold.TotalHours,
            _options.CheckInterval.TotalHours);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessApprovalTimeoutsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing approval timeouts");
            }

            await Task.Delay(_options.CheckInterval, stoppingToken);
        }

        _logger.LogInformation("ApprovalReminderService stopped");
    }

    private async Task ProcessApprovalTimeoutsAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var approvalService = scope.ServiceProvider.GetRequiredService<IApprovalService>();
        var workflowService = scope.ServiceProvider.GetRequiredService<IWorkflowInstanceService>();

        var (needReminder, timedOut) = await approvalService.GetTimedOutApprovalsAsync(
            _options.ReminderThreshold,
            _options.TimeoutThreshold,
            cancellationToken);

        foreach (var approval in needReminder)
        {
            await SendReminderNotificationAsync(approval, cancellationToken);
        }

        foreach (var approval in timedOut)
        {
            await HandleTimeoutAsync(approval, approvalService, workflowService, cancellationToken);
        }

        if (needReminder.Any() || timedOut.Any())
        {
            _logger.LogInformation(
                "Processed approval timeouts: {Reminders} reminders sent, {Timeouts} timed out",
                needReminder.Count, timedOut.Count);
        }
    }

    private async Task SendReminderNotificationAsync(ApprovalRequest approval, CancellationToken cancellationToken)
    {
        try
        {
            var hoursWaiting = (DateTime.UtcNow - approval.RequestedAt).TotalHours;
            
            await _hubContext.Clients.All.SendAsync("APPROVAL_REMINDER", new
            {
                ApprovalRequestId = approval.Id,
                WorkflowInstanceId = approval.WorkflowInstanceId,
                AgentId = approval.AgentId,
                ConfidenceScore = approval.ConfidenceScore,
                HoursWaiting = Math.Round(hoursWaiting, 1),
                RequestedAt = approval.RequestedAt,
                Message = $"Reminder: An approval request has been waiting for {hoursWaiting:F0} hours"
            }, cancellationToken);

            _logger.LogInformation(
                "Sent reminder for approval {ApprovalId} (waiting {Hours:F1} hours)",
                approval.Id, hoursWaiting);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send reminder for approval {ApprovalId}", approval.Id);
        }
    }

    private async Task HandleTimeoutAsync(
        ApprovalRequest approval,
        IApprovalService approvalService,
        IWorkflowInstanceService workflowService,
        CancellationToken cancellationToken)
    {
        try
        {
            var marked = await approvalService.MarkAsTimedOutAsync(approval.Id, cancellationToken);
            if (!marked)
            {
                return;
            }

            var (success, _) = await workflowService.PauseWorkflowAsync(
                approval.WorkflowInstanceId, 
                approval.RequestedBy);

            if (success)
            {
                await _hubContext.Clients.All.SendAsync("APPROVAL_TIMEOUT", new
                {
                    ApprovalRequestId = approval.Id,
                    WorkflowInstanceId = approval.WorkflowInstanceId,
                    AgentId = approval.AgentId,
                    RequestedAt = approval.RequestedAt,
                    TimedOutAt = DateTime.UtcNow,
                    Message = "Approval request timed out after 72 hours. Workflow has been paused."
                }, cancellationToken);

                _logger.LogWarning(
                    "Approval {ApprovalId} timed out, workflow {WorkflowId} paused",
                    approval.Id, approval.WorkflowInstanceId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to handle timeout for approval {ApprovalId}", approval.Id);
        }
    }
}
