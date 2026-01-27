namespace bmadServer.ApiService.Models.Events;

public class MessageReceivedEvent
{
    public string Message { get; set; } = string.Empty;
    public Guid MessageId { get; set; }
}
