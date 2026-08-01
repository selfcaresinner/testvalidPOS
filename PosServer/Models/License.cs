using System;
using System.ComponentModel.DataAnnotations;

namespace PosServer.Models;

public class License
{
    [Key]
    public int Id { get; set; }
    public string LicenseKey { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public int MaxTerminals { get; set; } = 1;
    public DateTime ValidUntil { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Timestamp]
    public byte[]? RowVersion { get; set; }
}
