using System.IO;
using System.Windows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using PosCore.Data;
using PosCore.Models;
using PosCore.Services;
using PosCore.ViewModels;
using PosCore.Views;
using Squirrel;
using System.Threading.Tasks;
using Serilog;
using System;
using System.Linq;

namespace PosCore;

public partial class App : Application
{
    public App()
    {
        QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
    }
    private async Task CheckForUpdatesAsync()
    {
        try
        {
            var updateUrl = Environment.GetEnvironmentVariable("POS_UPDATE_URL") ?? "https://pos-service-production-ad3c.up.railway.app/releases";
            using (var mgr = new UpdateManager(updateUrl))
            {
                if (mgr.IsInstalledApp)
                {
                    await mgr.UpdateApp();
                }
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Error al actualizar la aplicación.");
        }
    }
    public static IServiceProvider? ServiceProvider { get; private set; }

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Global Exception Handlers
        this.DispatcherUnhandledException += (s, args) =>
        {
            Log.Fatal(args.Exception, "Unhandled UI exception");
            MessageBox.Show($"Ha ocurrido un error inesperado: {args.Exception.Message}\n\nRevisa el archivo de logs para más detalles.", "Error Fatal", MessageBoxButton.OK, MessageBoxImage.Error);
            args.Handled = true;
        };

        AppDomain.CurrentDomain.UnhandledException += (s, args) =>
        {
            Log.Fatal(args.ExceptionObject as Exception, "Unhandled Domain exception");
            MessageBox.Show($"Ha ocurrido un error fatal: {(args.ExceptionObject as Exception)?.Message}\n\nRevisa el archivo de logs para más detalles.", "Error Fatal", MessageBoxButton.OK, MessageBoxImage.Error);
        };

        // Configuración de Logging (Serilog)
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.File("logs/pos-log-.txt", rollingInterval: RollingInterval.Day)
            .CreateLogger();

        Log.Information("Iniciando aplicación Super POS Express...");

#if !DEBUG
        // 1. Manejar eventos de Squirrel (accesos directos al instalar/desinstalar)
        try 
        {
            var updateUrl = Environment.GetEnvironmentVariable("POS_UPDATE_URL") ?? "https://pos-service-production-ad3c.up.railway.app/releases";
            using (var mgr = new UpdateManager(updateUrl))
            {
                SquirrelAwareApp.HandleEvents(
                    onInitialInstall: (v, t) => mgr.CreateShortcutForThisExe(),
                    onAppUpdate: (v, t) => mgr.CreateShortcutForThisExe(),
                    onAppUninstall: (v, t) => mgr.RemoveShortcutForThisExe()
                    );
            }
            
            // 2. Comprobar actualizaciones en segundo plano
            _ = Task.Run(async () => await CheckForUpdatesAsync());
        } 
        catch 
        {
            // Ignorar errores si Squirrel falla o no hay conexión
        }
#endif

        string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
        var secureSettings = SecureConfigManager.LoadAndSecureConfig(configPath);
        
        var services = new ServiceCollection();

        // 0. Configuración de Logging
        services.AddLogging(loggingBuilder =>
        {
            loggingBuilder.AddSerilog(dispose: true);
        });

        // 0. Configuración (Opciones en memoria a partir de los datos seguros)
        services.AddSingleton(Microsoft.Extensions.Options.Options.Create(secureSettings));

        // 1. Inyección del DbContext (EF Core SQLite)
        services.AddDbContext<PosDbContext>(options =>
            options.UseSqlite(secureSettings.DatabaseSettings.ConnectionString));

        // 2. Inyección de HttpClient, Handler de Auth y Servicios
        services.AddSingleton<SessionManager>();
        services.AddTransient<AuthDelegatingHandler>();
        services.AddHttpClient<LicenseService>();
        services.AddHttpClient<IApiService, ApiService>()
            .AddHttpMessageHandler<AuthDelegatingHandler>();

        // 3. Inyección de ViewModels
        services.AddTransient<MainViewModel>();
        services.AddTransient<InventoryViewModel>();
        services.AddTransient<LoginViewModel>();
        services.AddTransient<ReportsViewModel>();
        services.AddTransient<ReturnsViewModel>();
        services.AddTransient<ShiftViewModel>();
        services.AddTransient<ShiftWindow>();
        services.AddTransient<UsersViewModel>();
        services.AddTransient<UsersWindow>();
        services.AddTransient<LogViewerViewModel>();
        services.AddTransient<LogViewerWindow>();

        // 4. Inyección del servicio de sincronización (Singleton)
        services.AddSingleton<SyncService>();
        services.AddSingleton<TicketPrinterService>();

        // 5. Inyección de Views
        services.AddTransient<MainWindow>();
        services.AddTransient<InventoryWindow>();
        services.AddTransient<LoginWindow>();
        services.AddTransient<ReportsWindow>();
        services.AddTransient<ReturnsWindow>();

        ServiceProvider = services.BuildServiceProvider();


        // Aplicar migraciones y Backup
        using (var scope = ServiceProvider.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<PosDbContext>();
            dbContext.Database.EnsureCreated();
            dbContext.InitializeDatabaseSettings();
            var connStr = secureSettings.DatabaseSettings.ConnectionString;
            
            DatabaseBackupService.ManageDatabaseBackup(connStr);

            try 
            {
                try {
                    
                    








                    














                    try {
                        dbContext.Database.ExecuteSqlRaw(@"
                            CREATE TABLE IF NOT EXISTS CashMovements (
                                Id INTEGER NOT NULL CONSTRAINT PK_CashMovements PRIMARY KEY AUTOINCREMENT,
                                ShiftId INTEGER NOT NULL,
                                Type TEXT NOT NULL,
                                Amount TEXT NOT NULL,
                                Reason TEXT NOT NULL,
                                CreatedAt TEXT NOT NULL,
                                TenantId TEXT NOT NULL
                            );");
                    } catch { }

                    if (dbContext.Database.GetPendingMigrations().Any())
                    {
                        dbContext.Database.Migrate();
                    }
                } catch { }
                
                // Seed inicial
                try {

                    if (!Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.IgnoreQueryFilters(dbContext.Products).Any())
                    {
                        dbContext.Products.AddRange(
                            new PosCore.Models.Product { Name = "Coca Cola 600ml", Barcode = "7501055300075", Price = 18.00m, StockQuantity = 50, Category = "Bebidas", MinStockThreshold = 10, TenantId = "LOCAL", LastUpdated = System.DateTime.Now },
                            new PosCore.Models.Product { Name = "Sabritas Sal 40g", Barcode = "7501011111111", Price = 15.00m, StockQuantity = 30, Category = "Botanas", MinStockThreshold = 10, TenantId = "LOCAL", LastUpdated = System.DateTime.Now },
                            new PosCore.Models.Product { Name = "Agua Ciel 1L", Barcode = "7501022222222", Price = 12.00m, StockQuantity = 40, Category = "Bebidas", MinStockThreshold = 10, TenantId = "LOCAL", LastUpdated = System.DateTime.Now }
                        );
                        dbContext.SaveChanges();
                    }
                } catch { }

            } 
            catch (Microsoft.Data.Sqlite.SqliteException ex) when (ex.SqliteErrorCode == 11 || ex.SqliteErrorCode == 26 || ex.Message.Contains("malformed"))
            {
                // 11 = SQLITE_CORRUPT, 26 = SQLITE_NOTADB
                Log.Error(ex, "Base de datos corrupta detectada.");
                if (DatabaseBackupService.TryRestoreFromBackup(connStr))
                {
                    Application.Current.Shutdown();
                    return;
                }
                else 
                {
                    MessageBox.Show("No se pudo reparar la base de datos. Póngase en contacto con el soporte.", "Error fatal", MessageBoxButton.OK, MessageBoxImage.Error);
                    Application.Current.Shutdown();
                    return;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error al aplicar migraciones de base de datos.");
            }
        }

        Application.Current.ShutdownMode = ShutdownMode.OnExplicitShutdown;

        var sessionManager = ServiceProvider.GetRequiredService<SessionManager>();
        bool isLoggedIn = sessionManager.LoadSession();

        if (!isLoggedIn)
        {
            var loginWindow = ServiceProvider.GetRequiredService<LoginWindow>();
            isLoggedIn = loginWindow.ShowDialog() == true;
        }

        if (isLoggedIn)
        {
            var licenseService = ServiceProvider.GetRequiredService<LicenseService>();
            bool isLicenseValid = await licenseService.ValidateLicenseAsync();
            if (!isLicenseValid)
            {
                Application.Current.Shutdown();
                return;
            }

            var syncService = ServiceProvider.GetRequiredService<SyncService>();
            syncService.Start();
            
            var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
            Application.Current.MainWindow = mainWindow;
            Application.Current.ShutdownMode = ShutdownMode.OnMainWindowClose;
            mainWindow.Show();
        }
        else
        {
            Application.Current.Shutdown();
        }
    }
}
