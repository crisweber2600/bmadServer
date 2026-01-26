namespace bmadServer.ApiService.Models.Events;

public class PresenceEvent
{
    public bool IsOnline { get; set; }
    public DateTime LastSeen { get; set; }
}
