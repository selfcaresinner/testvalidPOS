using System.Security.Claims;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PosServer.Data;
using PosServer.Models;
using BCrypt.Net;
using System.Net;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.FileProviders;
using System.IO;
using System;

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<PosServer.Services.ITenantService, PosServer.Services.TenantService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure Database
var connString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "";
var envDbUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
if (!string.IsNullOrEmpty(envDbUrl)) {
    connString = envDbUrl;
}
if (connString.StartsWith("\"") && connString.EndsWith("\"")) {
    connString = connString.Trim('"');
}
if (connString.StartsWith("postgres://") || connString.StartsWith("postgresql://")) {
    var uri = new Uri(connString);
    var userInfo = uri.UserInfo.Split(':', 2); // Limit split to 2 in case password has colon
    var username = WebUtility.UrlDecode(userInfo[0]);
    var password = userInfo.Length > 1 ? WebUtility.UrlDecode(userInfo[1]) : "";
    connString = $"Host={uri.Host};Port={(uri.IsDefaultPort ? 5432 : uri.Port)};Database={uri.LocalPath.TrimStart('/')};Username={username};Password={password};SSL Mode=Require;Trust Server Certificate=True";
}

if (builder.Configuration.GetValue<bool>("EnableLegacyTimestamp"))
{
    AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
}
if ((connString.Contains("supabase.com") || connString.Contains("pooler")) && builder.Configuration.GetValue<bool>("ApplySupabaseFix", true))
{
    // Fix for Supabase Transaction Pooler (pgbouncer) which breaks EF Core Prepared Statements
    if (!connString.Contains("Max Auto Prepare"))
    {
        connString += ";Max Auto Prepare=0;Pooling=false;";
    }
}

builder.Services.AddDbContext<CentralDbContext>(options =>
    options.UseNpgsql(connString, o => {
        o.CommandTimeout(120);
        o.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(10), errorCodesToAdd: null);
    }));

// Configure JWT Authentication
var jwtKey = Environment.GetEnvironmentVariable("JWT_KEY") ?? builder.Configuration["Jwt:Key"];
if (string.IsNullOrEmpty(jwtKey)) throw new InvalidOperationException("JWT_KEY no configurada");
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey!))
        };
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var dbContext = context.HttpContext.RequestServices.GetRequiredService<CentralDbContext>();
                var username = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
                var tenantId = context.Principal?.FindFirstValue("TenantId");
                if (!string.IsNullOrEmpty(username))
                {
                    var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Username == username && (tenantId == null || u.TenantId == tenantId));
                    if (user == null || !user.IsActive)
                    {
                        context.Fail("Usuario inactivo o revocado.");
                    }
                }
            }
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? httpContext.Request.Headers.Host.ToString(),
            factory: partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 100,
                QueueLimit = 0,
                Window = TimeSpan.FromMinutes(1)
            }));

    options.AddFixedWindowLimiter("LoginPolicy", opt =>
    {
        opt.Window = TimeSpan.FromMinutes(1);
        opt.PermitLimit = 20; // Increased permit limit for login
        opt.QueueLimit = 0;
    });
    options.RejectionStatusCode = 429;
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        var allowedOrigins = Environment.GetEnvironmentVariable("ALLOWED_ORIGINS")?.Split(',') ?? new[] { "https://trusted-domain.com" };
        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});
var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseCors("AllowAll");
app.UseRateLimiter();
app.UseExceptionHandler(exceptionHandlerApp =>
{
    exceptionHandlerApp.Run(async context =>
    {
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/json";

        var exceptionHandlerPathFeature = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerPathFeature>();
        var errorMessage = exceptionHandlerPathFeature?.Error.Message ?? "Error interno del servidor.";

        var isDevelopment = builder.Environment.IsDevelopment();
        var stackTrace = isDevelopment ? exceptionHandlerPathFeature?.Error.StackTrace : null;
        await context.Response.WriteAsJsonAsync(new { error = errorMessage, details = stackTrace });
    });
});
app.UseSwagger();
app.UseSwaggerUI();
app.MapGet("/", () => "POS Server is running!");

// Servir la carpeta releases estáticamente para Squirrel
var releasesPath = Path.Combine(builder.Environment.ContentRootPath, "releases");
if (!Directory.Exists(releasesPath))
{
    Directory.CreateDirectory(releasesPath);
}

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(releasesPath),
    RequestPath = "/releases",
    ServeUnknownFileTypes = true // Importante para .nupkg y RELEASES
});

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseMiddleware<PosServer.Middlewares.TenantMiddleware>();
app.UseAuthorization();
app.MapControllers();

// Init Database
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<CentralDbContext>();
    
    // Attempt to create tables if they don't exist
    try
    {
        var creator = dbContext.Database.GetService<IRelationalDatabaseCreator>();
        if (!creator.Exists())
        {
            creator.Create();
        }
        creator.CreateTables();
    }
    catch
    {
    }

    try { dbContext.Database.ExecuteSqlRaw("ALTER TABLE \"Users\" ADD COLUMN IF NOT EXISTS \"PasswordHash\" text DEFAULT '';"); } catch { }
    try { dbContext.Database.ExecuteSqlRaw("CREATE EXTENSION IF NOT EXISTS pgcrypto;"); } catch { }
    try { dbContext.Database.ExecuteSqlRaw("UPDATE \"Users\" SET \"PasswordHash\" = crypt(\"Pin\", gen_salt('bf')) WHERE \"Pin\" IS NOT NULL AND \"Pin\" != '';"); } catch { }
    try { dbContext.Database.ExecuteSqlRaw("ALTER TABLE \"Users\" DROP COLUMN IF EXISTS \"Pin\";"); } catch { }
}



var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
app.Run($"http://0.0.0.0:{port}");
