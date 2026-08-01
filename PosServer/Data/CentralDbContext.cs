using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore;
using PosServer.Models;
using PosServer.Services;
using System.Text.Json;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace PosServer.Data;

public class CentralDbContext : DbContext
{
    private readonly ITenantService? _tenantService;

    public string CurrentTenantId 
    { 
        get 
        {
            var id = _tenantService?.GetTenantId() ?? string.Empty;
            if (string.IsNullOrEmpty(id))
            {
                throw new InvalidOperationException("CurrentTenantId is not set. Check TenantMiddleware configuration.");
            }
            return id;
        }
    }

    public CentralDbContext(DbContextOptions<CentralDbContext> options, ITenantService tenantService) : base(options)
    {
        _tenantService = tenantService;
    }

    // Constructor sin ITenantService para herramientas de diseño
    public CentralDbContext(DbContextOptions<CentralDbContext> options) : base(options)
    {
    }

    public DbSet<Product> Products { get; set; } = null!;
    public DbSet<Order> Orders { get; set; } = null!;
    public DbSet<OrderItem> OrderItems { get; set; } = null!;
    public DbSet<User> Users { get; set; } = null!;
    public DbSet<License> Licenses { get; set; } = null!;
    public DbSet<ProductModifier> ProductModifiers { get; set; } = null!;
    public DbSet<ModifierOption> ModifierOptions { get; set; } = null!;
    public DbSet<ProductModifierLink> ProductModifierLinks { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Conversor para Dictionary<string, object> a JSON string (que en Postgres se mapeará a jsonb)
        var dictConverter = new ValueConverter<Dictionary<string, object>, string>(
            v => JsonSerializer.Serialize(v, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }),
            v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }) ?? new Dictionary<string, object>()
        );
        var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        var dictComparer = new ValueComparer<Dictionary<string, object>>(
            (c1, c2) => JsonSerializer.Serialize(c1, jsonOptions) == JsonSerializer.Serialize(c2, jsonOptions),
            c => c == null ? 0 : JsonSerializer.Serialize(c, jsonOptions).GetHashCode(),
            c => c == null ? new Dictionary<string, object>() : JsonSerializer.Deserialize<Dictionary<string, object>>(JsonSerializer.Serialize(c, jsonOptions), jsonOptions) ?? new Dictionary<string, object>()
        );

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Username)
            .IsUnique();
        modelBuilder.Entity<User>()
            .HasIndex(u => u.TenantId);
        
        modelBuilder.Entity<Product>()
            .HasIndex(p => new { p.TenantId, p.Barcode })
            .IsUnique();
        modelBuilder.Entity<Product>()
            .HasIndex(p => p.TenantId);
        
        modelBuilder.Entity<Order>()
            .HasIndex(o => new { o.TenantId, o.OrderDate })
            .IsDescending(false, true);
        
        modelBuilder.Entity<OrderItem>()
            .HasIndex(oi => oi.OrderId);

        modelBuilder.Entity<Order>()
            .HasMany(o => o.Items)
            .WithOne(i => i.Order)
            .HasForeignKey(i => i.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configuración de CustomAttributes y Filtros Globales (Global Query Filters)
        
        modelBuilder.Entity<Product>(entity => {
            entity.HasQueryFilter(e => e.TenantId == CurrentTenantId);
            entity.Property(e => e.CustomAttributes)
                  
                  .HasConversion(dictConverter, dictComparer);
        });

        modelBuilder.Entity<Order>(entity => {
            entity.HasQueryFilter(e => e.TenantId == CurrentTenantId);
            entity.Property(e => e.CustomAttributes)
                  
                  .HasConversion(dictConverter, dictComparer);
        });

        modelBuilder.Entity<OrderItem>(entity => {
            entity.HasQueryFilter(e => e.TenantId == CurrentTenantId);
            entity.Property(e => e.CustomAttributes)
                  
                  .HasConversion(dictConverter, dictComparer);
        });

        modelBuilder.Entity<User>(entity => {
            entity.HasQueryFilter(e => e.TenantId == CurrentTenantId);
        });

        modelBuilder.Entity<License>(entity => {
            entity.HasQueryFilter(e => e.TenantId == CurrentTenantId);
        });

        modelBuilder.Entity<ProductModifier>(entity => {
            entity.HasQueryFilter(e => e.TenantId == CurrentTenantId);
        });
        
        modelBuilder.Entity<ModifierOption>(entity => {
            entity.HasQueryFilter(e => e.TenantId == CurrentTenantId);
        });

        modelBuilder.Entity<ProductModifierLink>(entity => {
            entity.HasQueryFilter(e => e.TenantId == CurrentTenantId);
        });
    }
}
