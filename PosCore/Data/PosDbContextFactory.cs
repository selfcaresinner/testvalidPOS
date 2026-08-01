using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using PosCore.Services;

namespace PosCore.Data;

public class PosDbContextFactory : IDesignTimeDbContextFactory<PosDbContext>
{
    public PosDbContext CreateDbContext(string[] args)
    {
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json")
            .Build();

        var builder = new DbContextOptionsBuilder<PosDbContext>();
        var connectionString = configuration.GetSection("DatabaseSettings")["ConnectionString"];
        builder.UseSqlite(connectionString);

        var sessionManager = new SessionManager();
        sessionManager.CurrentTenantId = "TENANT_001"; // Default for design time

        return new PosDbContext(builder.Options, sessionManager);
    }
}
