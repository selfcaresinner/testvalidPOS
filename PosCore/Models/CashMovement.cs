using System;

namespace PosCore.Models;

public class CashMovement
{
    public int Id { get; set; }
    public int ShiftId { get; set; }
    
    // "Entrada" o "Salida"
    public string Type { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    
    [System.Text.Json.Serialization.JsonIgnore]
    public string TenantId { get; set; } = string.Empty;
}
