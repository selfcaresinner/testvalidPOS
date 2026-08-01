using System;
using System.Collections.Generic;

namespace PosServer.Models;

public class ProductModifier
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty; // e.g., "Tipo de Leche", "Temperatura"
    public string Description { get; set; } = string.Empty;
    public bool IsRequired { get; set; } = false;
    public int MinSelections { get; set; } = 0;
    public int MaxSelections { get; set; } = 1;
    
    public string TenantId { get; set; } = string.Empty;
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

    public List<ModifierOption> Options { get; set; } = new();
}

public class ModifierOption
{
    public int Id { get; set; }
    public int ProductModifierId { get; set; }
    public ProductModifier? ProductModifier { get; set; }
    
    public string Name { get; set; } = string.Empty; // e.g., "Deslactosada", "Almendra"
    public decimal PriceAdjustment { get; set; } = 0; // e.g., +15.00
    public bool IsDefault { get; set; } = false;
    public int SortOrder { get; set; } = 0;
    
    public string TenantId { get; set; } = string.Empty;
}

public class ProductModifierLink
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public Product? Product { get; set; }
    
    public int ProductModifierId { get; set; }
    public ProductModifier? ProductModifier { get; set; }
    
    public int SortOrder { get; set; } = 0;
    public string TenantId { get; set; } = string.Empty;
}
