using System.Collections.Generic;
using System;
namespace PosServer.Models;
public class Product
{
    public int Id { get; set; }
    public string? Name { get; set; } = string.Empty;
    public string? Barcode { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
    public int MinStockThreshold { get; set; } = 10;
    public string? Category { get; set; } = "General";
    
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    public string? TenantId { get; set; } = string.Empty;
    public Dictionary<string, object> CustomAttributes { get; set; } = new();
}
