namespace PosCore.Models;

public class LoginResponse
{
    public string Token { get; set; } = string.Empty;
    
    public string TenantId { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}
