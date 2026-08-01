using System;

namespace PosCore.Models;

public class CashRegisterShift
{
    public int Id { get; set; }
    public DateTime OpenedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    
    public string OpenedBy { get; set; } = string.Empty;
    public string? ClosedBy { get; set; }
    
    public decimal StartingCash { get; set; }
    
    public decimal? ExpectedEndingCash { get; set; }
    public decimal? ActualEndingCash { get; set; }
    public decimal? Difference { get; set; }
    
    public bool IsClosed { get; set; } = false;

    public DateTime LastUpdated { get; set; } = DateTime.Now;
    [System.Text.Json.Serialization.JsonIgnore]
    public string TenantId { get; set; } = string.Empty;
}
