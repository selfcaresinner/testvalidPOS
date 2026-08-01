using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using PosServer.Data;
using Microsoft.EntityFrameworkCore;
using PosServer.Models;
using PosServer.Services;

namespace PosServer.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly CentralDbContext _dbContext;
    private readonly ITenantService _tenantService;

    public AuthController(IConfiguration configuration, CentralDbContext dbContext, ITenantService tenantService)
    {
        _configuration = configuration;
        _dbContext = dbContext;
        _tenantService = tenantService;
    }

    [HttpPost("login")]
    [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("LoginPolicy")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var tenantId = _tenantService.GetTenantId();
        
        try 
        {
            // First we try to match by exact TenantId (if provided via Token, but this is a login so usually no token).
            // But we'll try it as requested. If tenantId is empty, it might mean the request has no token.
            // If the user's snippet insists on this, we'll include it.
            // Wait, their snippet used `&& u.Pin == request.Pin`. Our LoginRequest has `Password`.
            
            if (string.IsNullOrWhiteSpace(request?.Username) || string.IsNullOrWhiteSpace(request?.Password))
            {
                return BadRequest(new { Message = "Username y Password son requeridos." });
            }
            var user = await _dbContext.Users
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(u => (tenantId == "" || u.TenantId == tenantId)
                                       && u.Username.ToLower() == request.Username.ToLower() 
                                       && u.IsActive);
                        
            if (user != null && BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                var token = GenerateJwtToken(user.Username, user.TenantId);
                // Return exactly what the user requested: { user.Id, user.Username, user.Role, user.TenantId }
                // PLUS the Token so it continues to work with other clients
                return Ok(new { 
                    Token = token, 
                    TenantId = user.TenantId ?? "default",
                    user.Id, 
                    user.Username, 
                    user.Role 
                });
            }
            return Unauthorized(new { Message = "Credenciales inválidas o usuario no activo." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Error = "Error en la autenticación", Details = ex.Message });
        }
    }

    private string GenerateJwtToken(string username, string tenantId)
    {
        var jwtKey = Environment.GetEnvironmentVariable("JWT_KEY") ?? _configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT_KEY no configurada");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, username ?? "unknown"),
            new Claim("TenantId", tenantId ?? "default"),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(24),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
