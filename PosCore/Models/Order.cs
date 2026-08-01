using System.Collections.Generic;
namespace PosCore.Models;

public class Order
{
    public int Id { get; set; }
    public DateTime OrderDate { get; set; } = DateTime.Now;
    public string? CustomerName { get; set; } = string.Empty;
    public decimal SubTotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    
    // Relación
    public List<OrderItem> Items { get; set; } = new();
    
    // Bandera para saber si ya se sincronizó con la BD Central
    public bool IsSynced { get; set; } = false;
    
    public DateTime LastUpdated { get; set; } = DateTime.Now;
    public bool IsReturned { get; set; } = false;
    public string ReturnReason { get; set; } = string.Empty;
    public string AuthorizedBy { get; set; } = string.Empty;
    public string PaymentDetails { get; set; } = string.Empty;
    [System.Text.Json.Serialization.JsonIgnore]
    public string TenantId { get; set; } = string.Empty;
    public string ClientSideId { get; set; } = string.Empty;
    public Dictionary<string, object> CustomAttributes { get; set; } = new();
}
