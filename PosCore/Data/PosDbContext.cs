using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Text.Json;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PosCore.Models;
using PosCore.Services;

namespace PosCore.Data;

public class PosDbContext : DbContext
{
    private readonly SessionManager _sessionManager;

    public DbSet<Product> Products { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }
    public DbSet<OutboxMessage> OutboxMessages { get; set; }
    public DbSet<CashRegisterShift> CashRegisterShifts { get; set; }
    public DbSet<CashMovement> CashMovements { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<ProductModifier> ProductModifiers { get; set; }
    public DbSet<ModifierOption> ModifierOptions { get; set; }
    public DbSet<ProductModifierLink> ProductModifierLinks { get; set; }

    public PosDbContext(DbContextOptions<PosDbContext> options, SessionManager sessionManager) : base(options)
    {
        _sessionManager = sessionManager;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Optimización SQLite: Índices
        modelBuilder.Entity<Product>()
            .HasIndex(p => p.Barcode)
            .IsUnique();
        
        modelBuilder.Entity<Order>()
            .HasIndex(o => o.OrderDate);
            
        modelBuilder.Entity<OutboxMessage>()
            .HasIndex(om => new { om.ProcessedAt, om.CreatedAt });
            
        modelBuilder.Entity<CashRegisterShift>()
            .HasIndex(crs => crs.TenantId);
            
        modelBuilder.Entity<Order>()
            .HasMany(o => o.Items)
            .WithOne(oi => oi.Order)
            .HasForeignKey(oi => oi.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

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

        modelBuilder.Entity<Product>().Property(e => e.CustomAttributes).HasConversion(dictConverter, dictComparer);
        modelBuilder.Entity<Order>().Property(e => e.CustomAttributes).HasConversion(dictConverter, dictComparer);
        modelBuilder.Entity<OrderItem>().Property(e => e.CustomAttributes).HasConversion(dictConverter, dictComparer);

            
        // Multi-Tenant: Filtro Global
        modelBuilder.Entity<Product>().HasQueryFilter(e => e.TenantId == _sessionManager.CurrentTenantId);
        modelBuilder.Entity<Order>().HasQueryFilter(e => e.TenantId == _sessionManager.CurrentTenantId);
        modelBuilder.Entity<OrderItem>().HasQueryFilter(e => e.TenantId == _sessionManager.CurrentTenantId);
        modelBuilder.Entity<OutboxMessage>().HasQueryFilter(e => e.TenantId == _sessionManager.CurrentTenantId);
        modelBuilder.Entity<CashRegisterShift>().HasQueryFilter(e => e.TenantId == _sessionManager.CurrentTenantId);
        modelBuilder.Entity<CashMovement>().HasQueryFilter(e => e.TenantId == _sessionManager.CurrentTenantId);
        modelBuilder.Entity<User>().HasQueryFilter(e => e.TenantId == _sessionManager.CurrentTenantId);
        modelBuilder.Entity<ProductModifier>().HasQueryFilter(e => e.TenantId == _sessionManager.CurrentTenantId);
        modelBuilder.Entity<ModifierOption>().HasQueryFilter(e => e.TenantId == _sessionManager.CurrentTenantId);
        modelBuilder.Entity<ProductModifierLink>().HasQueryFilter(e => e.TenantId == _sessionManager.CurrentTenantId);
    }
    
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        AssignTenantIdToAddedEntities();
        UpdateLastUpdatedField();
        return base.SaveChangesAsync(cancellationToken);
    }
    
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            var connectionString = "Data Source=pos_local.db;Default Timeout=30;";
            optionsBuilder.UseSqlite(connectionString);
        }
    }

    public void InitializeDatabaseSettings()
    {
        Database.ExecuteSqlRaw("PRAGMA journal_mode=WAL;");
        Database.ExecuteSqlRaw("PRAGMA busy_timeout=5000;");
    }

    public override int SaveChanges()
    {
        AssignTenantIdToAddedEntities();
        UpdateLastUpdatedField();
        return base.SaveChanges();
    }

    private void AssignTenantIdToAddedEntities()
    {
        foreach (var entry in ChangeTracker.Entries().Where(e => e.State == EntityState.Added))
        {
            var tenantProperty = entry.Entity.GetType().GetProperty("TenantId");
            if (tenantProperty != null)
            {
                var currentValue = tenantProperty.GetValue(entry.Entity) as string;
                if (string.IsNullOrEmpty(currentValue))
                {
                    tenantProperty.SetValue(entry.Entity, _sessionManager.CurrentTenantId);
                }
            }
        }
    }

    private void UpdateLastUpdatedField()
    {
        foreach (var entry in ChangeTracker.Entries().Where(e => e.State == EntityState.Added || e.State == EntityState.Modified))
        {
            var lastUpdatedProperty = entry.Entity.GetType().GetProperty("LastUpdated");
            if (lastUpdatedProperty != null)
            {
                lastUpdatedProperty.SetValue(entry.Entity, DateTime.UtcNow);
            }
        }
    }
}
