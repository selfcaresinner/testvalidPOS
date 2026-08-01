using System.Collections.Generic;
namespace PosCore.Models;

public class OrderItem
{
    public int Id { get; set; }
    
    public int OrderId { get; set; }
    public Order Order { get; set; } = null!;
    
    public int ProductId { get; set; }
    public string ProductBarcode { get; set; } = string.Empty;
    public Product Product { get; set; } = null!;
    
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    
    // Propiedad calculada
    public decimal Discount { get; set; } = 0;
    public string Notes { get; set; } = string.Empty;
    public bool HasNotes => !string.IsNullOrWhiteSpace(Notes);
    public bool HasDiscount => Discount > 0;
    public decimal SubTotal => (Quantity * UnitPrice) - Discount;
    
    public DateTime LastUpdated { get; set; } = DateTime.Now;
    [System.Text.Json.Serialization.JsonIgnore]
    public string TenantId { get; set; } = string.Empty;
    public Dictionary<string, object> CustomAttributes { get; set; } = new();
}
