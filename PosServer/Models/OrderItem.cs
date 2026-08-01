using System.Collections.Generic;
using System;
namespace PosServer.Models;
public class OrderItem
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public Order? Order { get; set; }
    public int ProductId { get; set; }
    public string? ProductBarcode { get; set; } = string.Empty;
        
    public Product? Product { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }

    public decimal Discount { get; set; } = 0;
    public string? Notes { get; set; } = string.Empty;
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

    public string? TenantId { get; set; } = string.Empty;
    public Dictionary<string, object> CustomAttributes { get; set; } = new();
}
