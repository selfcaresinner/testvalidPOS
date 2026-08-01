using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using System.Threading;

namespace PosServer.Services;

public class TenantService : ITenantService
{
    private static readonly AsyncLocal<string> _tenantId = new AsyncLocal<string>();

    public void SetTenantId(string tenantId)
    {
        _tenantId.Value = tenantId;
    }

    public string GetTenantId()
    {
        return _tenantId.Value ?? string.Empty;
    }
}
