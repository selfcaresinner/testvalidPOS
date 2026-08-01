namespace PosServer.Services;

public interface ITenantService
{
    void SetTenantId(string tenantId);
    string GetTenantId();
}
