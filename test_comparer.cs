using System;
using System.Collections.Generic;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

public class TestDbContext : DbContext
{
    public DbSet<MyEntity> Entities { get; set; }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var converter = new ValueConverter<Dictionary<string, object>, string>(
            v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
            v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions)null) ?? new Dictionary<string, object>()
        );
        var comparer = new ValueComparer<Dictionary<string, object>>(
            (c1, c2) => JsonSerializer.Serialize(c1, (JsonSerializerOptions)null) == JsonSerializer.Serialize(c2, (JsonSerializerOptions)null),
            c => c == null ? 0 : JsonSerializer.Serialize(c, (JsonSerializerOptions)null).GetHashCode(),
            c => c == null ? null : JsonSerializer.Deserialize<Dictionary<string, object>>(JsonSerializer.Serialize(c, (JsonSerializerOptions)null), (JsonSerializerOptions)null)
        );
        modelBuilder.Entity<MyEntity>()
            .Property(e => e.Data)
            .HasConversion(converter, comparer);
    }
}
public class MyEntity
{
    public int Id { get; set; }
    public Dictionary<string, object> Data { get; set; }
}
