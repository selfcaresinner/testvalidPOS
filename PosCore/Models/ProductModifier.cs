using System;
using System.Collections.Generic;

namespace PosCore.Models;

public class ProductModifier
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsRequired { get; set; } = false;
    public int MinSelections { get; set; } = 0;
    public int MaxSelections { get; set; } = 1;
    
    [System.Text.Json.Serialization.JsonIgnore]
    public string TenantId { get; set; } = string.Empty;
    public DateTime LastUpdated { get; set; } = DateTime.Now;

    public List<ModifierOption> Options { get; set; } = new();
}

public class ModifierOption
{
    public int Id { get; set; }
    public int ProductModifierId { get; set; }
    public ProductModifier ProductModifier { get; set; } = null!;
    
    public string Name { get; set; } = string.Empty;
    public decimal PriceAdjustment { get; set; } = 0;
    public bool IsDefault { get; set; } = false;
    public int SortOrder { get; set; } = 0;
    
    [System.Text.Json.Serialization.JsonIgnore]
    public string TenantId { get; set; } = string.Empty;
}

public class ProductModifierLink
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    
    public int ProductModifierId { get; set; }
    public ProductModifier ProductModifier { get; set; } = null!;
    
    public int SortOrder { get; set; } = 0;
    
    [System.Text.Json.Serialization.JsonIgnore]
    public string TenantId { get; set; } = string.Empty;
}
