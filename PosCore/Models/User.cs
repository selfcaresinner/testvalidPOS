using System;

namespace PosCore.Models;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Pin { get; set; } = string.Empty; // For local login
    public string Role { get; set; } = "Cashier"; // "Admin" or "Cashier"
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    
    [System.Text.Json.Serialization.JsonIgnore]
    public string TenantId { get; set; } = string.Empty;
}
