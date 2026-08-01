namespace PosCore.Models;

public class OutboxMessage
{
    public int Id { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? ProcessedAt { get; set; }
    
    
    public int RetryCount { get; set; } = 0;
    public string TenantId { get; set; } = string.Empty;
}
