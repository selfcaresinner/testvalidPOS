namespace PosServer.Middlewares;

using Microsoft.AspNetCore.Http;
using PosServer.Services;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Linq;

public class TenantMiddleware
{
    private readonly RequestDelegate _next;

    public TenantMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ITenantService tenantService)
    {
        var tenantId = context.User?.FindFirstValue("TenantId");
        if (string.IsNullOrEmpty(tenantId))
        {
            tenantId = context.Request.Headers["X-Tenant-Id"].FirstOrDefault() ?? string.Empty;
        }

        var path = context.Request.Path.Value?.ToLower() ?? "";
        bool isExemptRoute = path.Contains("/api/auth/login") || path.Contains("/api/license/validate") || path.Contains("/api/license/generate");
        
        if (string.IsNullOrEmpty(tenantId) && !isExemptRoute)
        {
            context.Response.StatusCode = 400;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new { Error = "Falta la cabecera X-Tenant-Id o el claim de TenantId." });
            return;
        }
        
        tenantService.SetTenantId(tenantId);
        await _next(context);
    }
}
