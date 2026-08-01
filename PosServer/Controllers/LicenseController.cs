using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PosServer.Data;
using PosServer.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace PosServer.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LicenseController : ControllerBase
{
    private readonly CentralDbContext _context;

    public LicenseController(CentralDbContext context)
    {
        _context = context;
    }

    [HttpPost("validate")]
    [AllowAnonymous]
    public async Task<IActionResult> ValidateLicense([FromBody] LicenseRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.LicenseKey))
        {
            return BadRequest(new { IsValid = false, Error = "Clave de licencia vacía." });
        }

        var license = await _context.Licenses
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(l => l.LicenseKey == request.LicenseKey);

        if (license == null)
        {
            return Ok(new { IsValid = false, Error = "Licencia no encontrada." });
        }

        if (!license.IsActive)
        {
            return Ok(new { IsValid = false, Error = "La licencia está desactivada." });
        }

        if (license.ValidUntil.ToUniversalTime() < DateTime.UtcNow)
        {
            return Ok(new { IsValid = false, Error = "La licencia ha expirado." });
        }

        return Ok(new 
        { 
            IsValid = true, 
            MaxTerminals = license.MaxTerminals,
            ValidUntil = license.ValidUntil,
            TenantId = license.TenantId
        });
    }

    [HttpPost("generate")]
    [Authorize(Roles = "SuperAdmin")] // Require JWT admin token
    public async Task<IActionResult> GenerateLicense([FromBody] GenerateLicenseRequest request)
    {
        // En un caso real, validaríamos que el usuario autenticado tiene rol de SuperAdmin
        var newLicense = new License
        {
            LicenseKey = "VAL-" + Guid.NewGuid().ToString("N").Substring(0, 16).ToUpper(),
            TenantId = request.TenantId ?? "TENANT_001",
            Description = request.Description ?? "Licencia Generada Manualmente",
            IsActive = true,
            MaxTerminals = request.MaxTerminals > 0 ? request.MaxTerminals : 1,
            ValidUntil = request.DurationDays > 0 ? DateTime.UtcNow.AddDays(request.DurationDays) : DateTime.UtcNow.AddYears(1),
            CreatedAt = DateTime.UtcNow
        };

        _context.Licenses.Add(newLicense);
        await _context.SaveChangesAsync();

        return Ok(new { 
            Message = "Licencia generada exitosamente.",
            LicenseKey = newLicense.LicenseKey,
            ValidUntil = newLicense.ValidUntil,
            TenantId = newLicense.TenantId
        });
    }
}

public class LicenseRequest
{
    public string LicenseKey { get; set; } = string.Empty;
    public string TerminalId { get; set; } = string.Empty;
}

public class GenerateLicenseRequest
{
    public string? TenantId { get; set; }
    public string? Description { get; set; }
    public int MaxTerminals { get; set; } = 1;
    public int DurationDays { get; set; } = 365;
}
