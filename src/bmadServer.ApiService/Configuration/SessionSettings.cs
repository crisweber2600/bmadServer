namespace bmadServer.ApiService.Configuration;

public class SessionSettings
{
    public const string SectionName = "Session";
    
    public int RecoveryWindowSeconds { get; set; } = 60;
    
    public int IdleTimeoutMinutes { get; set; } = 30;
    
    public int WarningTimeoutMinutes { get; set; } = 28;
}
